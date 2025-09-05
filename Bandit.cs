using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PspRouter;

public interface IBandit
{
    string Select(string segmentKey, IReadOnlyList<string> arms);
    void Update(string segmentKey, string arm, double reward);
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
