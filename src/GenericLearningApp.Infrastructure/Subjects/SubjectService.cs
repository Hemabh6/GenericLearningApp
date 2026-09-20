using GenericLearningApp.Application.Subjects;
using GenericLearningApp.Domain.Entities;
using GenericLearningApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Infrastructure.Subjects;

/// <summary>
/// Reads use a fresh context per call because Blazor Server components are long-lived:
/// a scoped context would outlive the work it was made for.
/// </summary>
public class SubjectService(IDbContextFactory<LearningDbContext> factory) : ISubjectService
{
    public async Task<IReadOnlyList<SubjectListItem>> ListAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Subjects
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectListItem(s.Id, s.Name, s.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<Guid> CreateAsync(string userId, string name, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var subject = new Subject(userId, name);
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);
        return subject.Id;
    }

    public async Task<SubjectSummary?> GetSummaryAsync(string userId, Guid subjectId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var subject = await db.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == subjectId && s.UserId == userId, ct);

        if (subject is null) return null;

        var items = await db.ContentItems.AsNoTracking()
            .Where(i => i.SubjectId == subjectId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Done = g.Count(i => i.IsDone) })
            .SingleOrDefaultAsync(ct);

        var tasks = await db.PlanTasks.AsNoTracking()
            .Where(t => t.Plan!.SubjectId == subjectId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Done = g.Count(t => t.IsDone) })
            .SingleOrDefaultAsync(ct);

        var noteCount = await db.Notes.AsNoTracking()
            .CountAsync(n => n.SubjectId == subjectId, ct);

        var best = await db.TestAttempts.AsNoTracking()
            .Where(a => a.QuestionSet!.SubjectId == subjectId)
            .OrderByDescending(a => a.Score)
            .Select(a => new { a.Score, a.TotalQuestions })
            .FirstOrDefaultAsync(ct);

        return new SubjectSummary(
            subject.Id,
            subject.Name,
            items?.Done ?? 0,
            items?.Total ?? 0,
            tasks?.Done ?? 0,
            tasks?.Total ?? 0,
            noteCount,
            best?.Score,
            best?.TotalQuestions);
    }

    public async Task<bool> RenameAsync(string userId, Guid subjectId, string name, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var subject = await db.Subjects.SingleOrDefaultAsync(s => s.Id == subjectId && s.UserId == userId, ct);
        if (subject is null) return false;

        subject.Rename(name);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, Guid subjectId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var subject = await db.Subjects.SingleOrDefaultAsync(s => s.Id == subjectId && s.UserId == userId, ct);
        if (subject is null) return false;

        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> OwnsAsync(string userId, Guid subjectId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId && s.UserId == userId, ct);
    }
}
