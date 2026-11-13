using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Application.Features.Students.Abstractions;

public interface IStudentRepository
{
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> AcademicProgramIsActiveAsync(Guid academicProgramId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPageAsync(
        int page, int pageSize, Guid? academicProgramId, StudentStatus? status,
        CancellationToken cancellationToken = default);
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
}