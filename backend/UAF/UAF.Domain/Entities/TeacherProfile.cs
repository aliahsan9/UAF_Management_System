using UAF.Domain.Common;
namespace UAF.Domain.Entities;
/// <summary>Academic teaching attributes attached to an academic employee.</summary>
public sealed class TeacherProfile : AuditableEntity
{
 private TeacherProfile(){HighestQualification=Specialization=AcademicRank=string.Empty;}
 public TeacherProfile(Guid employeeId,string highestQualification,string specialization,string academicRank)
 {
  if(employeeId==Guid.Empty)throw new ArgumentException("Employee is required.");
  EmployeeId=employeeId;HighestQualification=Required(highestQualification,120);Specialization=Required(specialization,160);AcademicRank=Required(academicRank,100);
 }
 public Guid EmployeeId{get;private set;}
 public string HighestQualification{get;private set;}
 public string Specialization{get;private set;}
 public string AcademicRank{get;private set;}
 public void Update(string qualification,string specialization,string rank){HighestQualification=Required(qualification,120);Specialization=Required(specialization,160);AcademicRank=Required(rank,100);MarkUpdated();}
 private static string Required(string v,int max){if(string.IsNullOrWhiteSpace(v)||v.Trim().Length>max)throw new ArgumentException("A valid value is required.");return v.Trim();}
}