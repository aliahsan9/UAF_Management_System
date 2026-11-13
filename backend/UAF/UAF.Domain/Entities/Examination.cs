using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>A scheduled assessment belonging to one course offering.</summary>
public sealed class Examination : AuditableEntity
{
    private Examination() { Title = string.Empty; }
    public Examination(Guid courseOfferingId, string title, ExaminationType type, DateOnly scheduledDate, decimal maximumMarks, decimal passingMarks, decimal weightPercentage)
    {
        if (courseOfferingId == Guid.Empty) throw new ArgumentException("Course offering is required.", nameof(courseOfferingId));
        Title = NormalizeTitle(title);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        ValidateMarks(maximumMarks, passingMarks);
        if (scheduledDate == default) throw new ArgumentOutOfRangeException(nameof(scheduledDate));
        CourseOfferingId = courseOfferingId;
        Type = type;
        ScheduledDate = scheduledDate;
        MaximumMarks = maximumMarks;
        PassingMarks = passingMarks;
        Status = ExaminationStatus.Draft;
    }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public Guid CourseOfferingId { get; private set; }
    public string Title { get; private set; }
    public ExaminationType Type { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public decimal MaximumMarks { get; private set; }
    public decimal PassingMarks { get; private set; }
    public ExaminationStatus Status { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public void Schedule()
    {
        EnsureStatus(ExaminationStatus.Draft);
        Status = ExaminationStatus.Scheduled;
        MarkUpdated();
    }
    public void OpenMarksEntry()
    {
        EnsureStatus(ExaminationStatus.Scheduled);
        Status = ExaminationStatus.MarksEntryOpen;
        MarkUpdated();
    }
    public void Submit(Guid actorId, DateTimeOffset now)
    {
        EnsureStatus(ExaminationStatus.MarksEntryOpen);
        ValidateActorAndTime(actorId, now);
        Status = ExaminationStatus.Submitted;
        SubmittedByUserId = actorId;
        SubmittedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    public void Approve(Guid actorId, DateTimeOffset now)
    {
        EnsureStatus(ExaminationStatus.Submitted);
        ValidateActorAndTime(actorId, now);
        if (SubmittedByUserId == actorId) throw new InvalidOperationException("The submitter cannot approve their own examination results.");
        Status = ExaminationStatus.Approved;
        ApprovedByUserId = actorId;
        ApprovedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    public void Publish(Guid actorId, DateTimeOffset now)
    {
        EnsureStatus(ExaminationStatus.Approved);
        ValidateActorAndTime(actorId, now);
        Status = ExaminationStatus.Published;
        PublishedByUserId = actorId;
        PublishedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    public void Cancel()
    {
        if (Status is ExaminationStatus.Submitted or ExaminationStatus.Approved or ExaminationStatus.Published or ExaminationStatus.Cancelled)
            throw new InvalidOperationException("Submitted or finalized examination results cannot be cancelled.");
        Status = ExaminationStatus.Cancelled;
        MarkUpdated();
    }
    private void EnsureStatus(ExaminationStatus expected)
    {
        if (Status != expected) throw new InvalidOperationException($"Examination must be in {expected} state for this operation.");
    }
    private static void ValidateActorAndTime(Guid actorId, DateTimeOffset now)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Authenticated actor is required.", nameof(actorId));
        if (now == default) throw new ArgumentException("Operation timestamp is required.", nameof(now));
    }
    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 160) throw new ArgumentException("Examination title is required and cannot exceed 160 characters.", nameof(title));
        return title.Trim();
    }
    private static void ValidateMarks(decimal maximum, decimal passing)
    {
        if (maximum <= 0 || maximum > 10000) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (passing < 0 || passing > maximum) throw new ArgumentOutOfRangeException(nameof(passing));
    }
}
