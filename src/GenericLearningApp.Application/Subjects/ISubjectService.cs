namespace GenericLearningApp.Application.Subjects;

/// <summary>
/// Every method takes the owner's id and filters on it. No caller can reach another
/// user's subject, which is why the id is a parameter rather than ambient state.
/// </summary>
public interface ISubjectService
{
    Task<IReadOnlyList<SubjectListItem>> ListAsync(string userId, CancellationToken ct = default);

    Task<Guid> CreateAsync(string userId, string name, CancellationToken ct = default);

    /// <summary>Returns null when the subject does not exist or belongs to someone else.</summary>
    Task<SubjectSummary?> GetSummaryAsync(string userId, Guid subjectId, CancellationToken ct = default);

    Task<bool> RenameAsync(string userId, Guid subjectId, string name, CancellationToken ct = default);

    Task<bool> DeleteAsync(string userId, Guid subjectId, CancellationToken ct = default);

    Task<bool> OwnsAsync(string userId, Guid subjectId, CancellationToken ct = default);
}
