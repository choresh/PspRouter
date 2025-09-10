namespace PspRouter.Lib;

/// <summary>
/// Service for managing PSP candidates with performance tracking and learning
/// </summary>
public interface IPspCandidateProvider
{
    Task<IReadOnlyList<PspSnapshot>> GetCandidates(RouteInput transaction, CancellationToken cancellationToken = default);
    Task ProcessFeedback(TransactionFeedback feedback, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PspCandidate>> GetAllCandidates(CancellationToken cancellationToken = default);
    Task<PspCandidate?> GetCandidate(string pspName, CancellationToken cancellationToken = default);
}
