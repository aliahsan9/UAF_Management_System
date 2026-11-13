using UAF.Domain.Common;
using UAF.Domain.Enums;
namespace UAF.Domain.Entities;
/// <summary>Immutable, session-specific grading policy after publication.</summary>
public sealed class GradeScale : AuditableEntity
{
    private readonly List<GradeScaleBand> _bands = new();
    private GradeScale() { Name = string.Empty; }
    public GradeScale(Guid academicSessionId, string name, bool includeFailedCoursesInGpa, RepeatCoursePolicy repeatCoursePolicy, Guid configuredByUserId)
    {
        if (academicSessionId == Guid.Empty || configuredByUserId == Guid.Empty) throw new ArgumentException("Academic session and configuring user are required.");
        Name = NormalizeName(name);
        if (!Enum.IsDefined(repeatCoursePolicy)) throw new ArgumentOutOfRangeException(nameof(repeatCoursePolicy));
        AcademicSessionId = academicSessionId;
        IncludeFailedCoursesInGpa = includeFailedCoursesInGpa;
        RepeatCoursePolicy = repeatCoursePolicy;
        ConfiguredByUserId = configuredByUserId;
    }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public Guid AcademicSessionId { get; private set; }
    public string Name { get; private set; }
    public bool IncludeFailedCoursesInGpa { get; private set; }
    public RepeatCoursePolicy RepeatCoursePolicy { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public bool IsPublished { get; private set; }
    public IReadOnlyCollection<GradeScaleBand> Bands => _bands.AsReadOnly();
    public void AddBand(GradeScaleBand band)
    {
        ArgumentNullException.ThrowIfNull(band);
        if (IsPublished) throw new InvalidOperationException("A published grading scale is immutable.");
        if (band.GradeScaleId != Id) throw new ArgumentException("Grade band belongs to a different scale.", nameof(band));
        if (_bands.Any(x => x.LetterGrade.Equals(band.LetterGrade, StringComparison.OrdinalIgnoreCase) || x.MinimumPercentage == band.MinimumPercentage))
            throw new InvalidOperationException("Letter grades and minimum percentage thresholds must be unique.");
        _bands.Add(band);
        MarkUpdated();
    }
    public void Publish(Guid actorId, DateTimeOffset now)
    {
        if (IsPublished) throw new InvalidOperationException("Grading scale is already published.");
        if (actorId == Guid.Empty || now == default) throw new ArgumentException("Publishing actor and timestamp are required.");
        if (_bands.Count == 0 || !_bands.Any(x => x.MinimumPercentage == 0))
            throw new InvalidOperationException("A grading scale must contain grade bands starting at 0 percent.");
        if (_bands.Select(x => x.MinimumPercentage).Distinct().Count() != _bands.Count)
            throw new InvalidOperationException("Grade thresholds must be unique.");
        IsPublished = true;
        PublishedByUserId = actorId;
        PublishedAtUtc = now.ToUniversalTime();
        MarkUpdated(now);
    }
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120) throw new ArgumentException("Scale name is required and cannot exceed 120 characters.", nameof(name));
        return name.Trim();
    }
}
