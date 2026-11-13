using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>Configurable attendance calculation policy for an academic session.</summary>
public sealed class AttendancePolicy : AuditableEntity
{
    private AttendancePolicy() { }
    public AttendancePolicy(Guid academicSessionId, decimal requiredPercentage, decimal lateCreditPercentage, bool excludeExcusedFromDenominator, Guid configuredByUserId)
    {
        Validate(academicSessionId, requiredPercentage, lateCreditPercentage, configuredByUserId);
        AcademicSessionId = academicSessionId;
        RequiredPercentage = requiredPercentage;
        LateCreditPercentage = lateCreditPercentage;
        ExcludeExcusedFromDenominator = excludeExcusedFromDenominator;
        ConfiguredByUserId = configuredByUserId;
        ConfiguredAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid AcademicSessionId { get; private set; }
    public decimal RequiredPercentage { get; private set; }
    public decimal LateCreditPercentage { get; private set; }
    public bool ExcludeExcusedFromDenominator { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }
    public DateTimeOffset ConfiguredAtUtc { get; private set; }
    public void Update(decimal requiredPercentage, decimal lateCreditPercentage, bool excludeExcusedFromDenominator, Guid configuredByUserId)
    {
        Validate(AcademicSessionId, requiredPercentage, lateCreditPercentage, configuredByUserId);
        RequiredPercentage = requiredPercentage;
        LateCreditPercentage = lateCreditPercentage;
        ExcludeExcusedFromDenominator = excludeExcusedFromDenominator;
        ConfiguredByUserId = configuredByUserId;
        ConfiguredAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }
    private static void Validate(Guid sessionId, decimal required, decimal lateCredit, Guid actor)
    {
        if (sessionId == Guid.Empty || actor == Guid.Empty) throw new ArgumentException("Academic session and configuring actor are required.");
        if (required is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(required), "Required attendance percentage must be between 0 and 100.");
        if (lateCredit is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(lateCredit), "Late attendance credit must be between 0 and 100.");
    }
}
