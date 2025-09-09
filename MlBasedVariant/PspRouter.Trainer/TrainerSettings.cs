namespace PspRouter.Trainer;

public sealed class TrainerSettings
{
    // Total number of rows to sample (TOP N)
    public int TargetSampleSize { get; init; } = 50000;

    // Future use: control date window (months back)
    public int DateWindowMonths { get; init; } = 6;

    // Future use: per-segment max (per PSP/method/currency/status)
    public int MaxPerSegment { get; init; } = 50;

    // Future use: aim for balanced success/failure ratio
    public double TargetSuccessRatio { get; init; } = 0.5;
}


