using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class StudentFeeAccount : AuditableEntity
{
    private StudentFeeAccount() { }
    public StudentFeeAccount(Guid studentId) { if (studentId == Guid.Empty) throw new ArgumentException("Student is required."); StudentId = studentId; IsActive = true; }
    public Guid StudentId { get; private set; }
    public bool IsActive { get; private set; }
    public void Deactivate() { IsActive = false; MarkUpdated(); }
}