using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

public interface IBandit
{
    string Select(string segmentKey, IReadOnlyList<string> arms);
    void Update(string segmentKey, string arm, double reward);
}

/// <summary>
/// Contextual bandit that considers transaction context in addition to segment key
/// </summary>
public interface IContextualBandit : IBandit
{
    string SelectWithContext(string segmentKey, IReadOnlyList<string> arms, Dictionary<string, object> context);
    void UpdateWithContext(string segmentKey, string arm, double reward, Dictionary<string, object> context);
}

public sealed class EpsilonGreedyBandit : IBandit
{
    private readonly double _epsilon;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (double sum, int count)>> _stats = new();

    public EpsilonGreedyBandit(double epsilon = 0.1) => _epsilon = Math.Clamp(epsilon, 0.0, 1.0);

    public string Select(string segmentKey, IReadOnlyList<string> arms)
    {
        if (arms.Count == 0) throw new ArgumentException("No arms");
        if (RandomNumberGenerator.GetInt32(0, 1000) < (int)(_epsilon * 1000))
            return arms[RandomNumberGenerator.GetInt32(arms.Count)];

        var seg = _stats.GetOrAdd(segmentKey, _ => new());
        string best = arms[0];
        double bestAvg = double.NegativeInfinity;
        foreach (var a in arms)
        {
            var (sum, count) = seg.GetOrAdd(a, _ => (0.0, 0));
            var avg = count == 0 ? 0.0 : sum / count;
            if (avg > bestAvg) { bestAvg = avg; best = a; }
        }
        return best;
    }

    public void Update(string segmentKey, string arm, double reward)
    {
        var seg = _stats.GetOrAdd(segmentKey, _ => new());
        seg.AddOrUpdate(arm, _ => (reward, 1), (_, prev) => (prev.sum + reward, prev.count + 1));
    }
}

public sealed class ThompsonSamplingBandit : IBandit
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (double alpha, double beta)>> _params = new();

    public string Select(string segmentKey, IReadOnlyList<string> arms)
    {
        if (arms.Count == 0) throw new ArgumentException("No arms");
        var seg = _params.GetOrAdd(segmentKey, _ => new());
        string best = arms[0];
        double bestSample = double.NegativeInfinity;
        foreach (var a in arms)
        {
            var (alpha, beta) = seg.GetOrAdd(a, _ => (1.0, 1.0)); // Beta(1,1) prior
            double x = SampleGamma(alpha);
            double y = SampleGamma(beta);
            double draw = x / (x + y);
            if (draw > bestSample) { bestSample = draw; best = a; }
        }
        return best;
    }

    public void Update(string segmentKey, string arm, double reward)
    {
        bool success = reward > 0;
        var seg = _params.GetOrAdd(segmentKey, _ => new());
        seg.AddOrUpdate(arm, _ => success ? (2.0, 1.0) : (1.0, 2.0),
            (_, prev) => success ? (prev.alpha + 1, prev.beta) : (prev.alpha, prev.beta + 1));
    }

    private static double SampleGamma(double shape)
    {
        if (shape < 1) return SampleGamma(shape + 1) * Math.Pow(RandomDouble(), 1.0 / shape);
        double d = shape - 1.0 / 3.0;
        double c = 1.0 / Math.Sqrt(9.0 * d);
        while (true)
        {
            double x = Normal01();
            double v = 1.0 + c * x;
            if (v <= 0) continue;
            v = v * v * v;
            double u = RandomDouble();
            if (u < 1.0 - 0.0331 * (x * x) * (x * x)) return d * v;
            if (Math.Log(u) < 0.5 * x * x + d * (1 - v + Math.Log(v))) return d * v;
        }
    }

    private static double Normal01()
    {
        double u1 = RandomDouble();
        double u2 = RandomDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static double RandomDouble()
    {
        Span<byte> b = stackalloc byte[8];
        RandomNumberGenerator.Fill(b);
        ulong ul = BitConverter.ToUInt64(b);
        return (ul / (double)ulong.MaxValue);
    }
}

/// <summary>
/// Enhanced contextual bandit that considers transaction features for better decisions
/// </summary>
public sealed class ContextualEpsilonGreedyBandit : IContextualBandit
{
    private readonly double _epsilon;
    private readonly ILogger<ContextualEpsilonGreedyBandit>? _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (double sum, int count, Dictionary<string, double> features)>> _stats = new();

    public ContextualEpsilonGreedyBandit(double epsilon = 0.1, ILogger<ContextualEpsilonGreedyBandit>? logger = null)
    {
        _epsilon = Math.Clamp(epsilon, 0.0, 1.0);
        _logger = logger;
    }

    public string Select(string segmentKey, IReadOnlyList<string> arms)
    {
        return SelectWithContext(segmentKey, arms, new Dictionary<string, object>());
    }

    public string SelectWithContext(string segmentKey, IReadOnlyList<string> arms, Dictionary<string, object> context)
    {
        if (arms.Count == 0) throw new ArgumentException("No arms");
        
        // Extract context features
        var features = ExtractContextFeatures(context);
        
        if (RandomNumberGenerator.GetInt32(0, 1000) < (int)(_epsilon * 1000))
        {
            var selected = arms[RandomNumberGenerator.GetInt32(arms.Count)];
            _logger?.LogDebug("Contextual bandit exploration: selected {Arm} for segment {Segment}", selected, segmentKey);
            return selected;
        }

        var seg = _stats.GetOrAdd(segmentKey, _ => new());
        string best = arms[0];
        double bestScore = double.NegativeInfinity;
        
        foreach (var arm in arms)
        {
            var (sum, count, armFeatures) = seg.GetOrAdd(arm, _ => (0.0, 0, new Dictionary<string, double>()));
            
            // Calculate contextual score
            var score = CalculateContextualScore(sum, count, features, armFeatures);
            
            if (score > bestScore)
            {
                bestScore = score;
                best = arm;
            }
        }
        
        _logger?.LogDebug("Contextual bandit exploitation: selected {Arm} with score {Score} for segment {Segment}", best, bestScore, segmentKey);
        return best;
    }

    public void Update(string segmentKey, string arm, double reward)
    {
        UpdateWithContext(segmentKey, arm, reward, new Dictionary<string, object>());
    }

    public void UpdateWithContext(string segmentKey, string arm, double reward, Dictionary<string, object> context)
    {
        var seg = _stats.GetOrAdd(segmentKey, _ => new());
        var features = ExtractContextFeatures(context);
        
        seg.AddOrUpdate(arm, _ => (reward, 1, features), (_, prev) =>
        {
            // Update statistics
            var newSum = prev.sum + reward;
            var newCount = prev.count + 1;
            
            // Update feature averages (exponential moving average)
            var alpha = 0.1; // Learning rate for features
            var newFeatures = new Dictionary<string, double>(prev.features);
            
            foreach (var (key, value) in features)
            {
                if (newFeatures.ContainsKey(key))
                {
                    newFeatures[key] = (1 - alpha) * newFeatures[key] + alpha * value;
                }
                else
                {
                    newFeatures[key] = value;
                }
            }
            
            return (newSum, newCount, newFeatures);
        });
        
        _logger?.LogDebug("Updated contextual bandit: segment={Segment}, arm={Arm}, reward={Reward}", segmentKey, arm, reward);
    }

    private static Dictionary<string, double> ExtractContextFeatures(Dictionary<string, object> context)
    {
        var features = new Dictionary<string, double>();
        
        foreach (var (key, value) in context)
        {
            switch (value)
            {
                case double d:
                    features[key] = d;
                    break;
                case int i:
                    features[key] = i;
                    break;
                case decimal dec:
                    features[key] = (double)dec;
                    break;
                case bool b:
                    features[key] = b ? 1.0 : 0.0;
                    break;
                case string s when double.TryParse(s, out var parsed):
                    features[key] = parsed;
                    break;
                default:
                    // Convert other types to hash-based numeric features
                    features[key] = Math.Abs(value.GetHashCode()) % 1000 / 1000.0;
                    break;
            }
        }
        
        return features;
    }

    private static double CalculateContextualScore(double sum, int count, Dictionary<string, double> contextFeatures, Dictionary<string, double> armFeatures)
    {
        if (count == 0) return 0.0;
        
        var baseScore = sum / count;
        
        // Add contextual bonus based on feature similarity
        var contextualBonus = 0.0;
        foreach (var (key, value) in contextFeatures)
        {
            if (armFeatures.TryGetValue(key, out var armValue))
            {
                // Similarity bonus (higher when context matches arm's successful context)
                var similarity = 1.0 - Math.Abs(value - armValue);
                contextualBonus += similarity * 0.1; // Small bonus for context matching
            }
        }
        
        return baseScore + contextualBonus;
    }
}
