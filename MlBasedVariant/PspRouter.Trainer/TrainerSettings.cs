namespace PspRouter.Trainer;

public sealed class TrainerSettings
{
    // Data sampling settings
    public int TargetSampleSize { get; init; } = 50000;
    public int DateWindowMonths { get; init; } = 6;
    public int MaxPerSegment { get; init; } = 50;
    public double TargetSuccessRatio { get; init; } = 0.5;
    
    // Model training settings
    public string ModelOutputPath { get; init; } = "models/psp_routing_model.zip";
    public int MaxIterations { get; init; } = 1000;
    public double LearningRate { get; init; } = 0.1;
    public int NumLeaves { get; init; } = 31;
    public int MinDataInLeaf { get; init; } = 20;
    public double FeatureFraction { get; init; } = 0.8;
    public double BaggingFraction { get; init; } = 0.8;
    public int BaggingFreq { get; init; } = 5;
    public double ValidationFraction { get; init; } = 0.2;
    public int EarlyStoppingRounds { get; init; } = 50;
    public int Seed { get; init; } = 42;
    
    // Training objective
    public string Objective { get; init; } = "binary";
    public string Metric { get; init; } = "binary_logloss";
}


