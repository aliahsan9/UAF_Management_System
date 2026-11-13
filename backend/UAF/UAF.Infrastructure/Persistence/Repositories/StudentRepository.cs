using Microsoft.EntityFrameworkCore;
using UAF.Application.Common.Models;
using UAF.Application.Features.Students.Abstractions;
using UAF.Domain.Entities;
using UAF.Domain.Enums;

namespace UAF.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly UafDbContext _dbContext;

    public StudentRepository(UafDbContext dbContext) =>
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public Task<bool> RegistrationNumberExistsAsync(
        string registrationNumber, Guid? excludingId = null, CancellationToken ct = default) =>
        _dbContext.Students.AnyAsync(
            x => x.RegistrationNumber == registrationNumber &&
                 (!excludingId.HasValue || x.Id != excludingId.Value), ct);

    public Task<bool> AcademicProgramIsActiveAsync(Guid academicProgramId, CancellationToken ct = default) =>
        _dbContext.AcademicPrograms.AnyAsync(x => x.Id == academicProgramId && x.IsActive, ct);

    public async Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPageAsync(
        int page, int pageSize, Guid? academicProgramId, StudentStatus? status,
        string? search, string sortBy, string sortDirection, CancellationToken ct = default)
    {
        var query = _dbContext.Students.AsNoTracking();

        if (academicProgramId.HasValue)
            query = query.Where(x => x.AcademicProgramId == academicProgramId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.RegistrationNumber.Contains(term) ||
                                     x.FullName.Contains(term) || x.Email.Contains(term));
        }
        var totalCount = await query.CountAsync(ct);
        IOrderedQueryable<Student> ordered = (sortBy, sortDirection) switch
        {
            ("fullname", "desc") => query.OrderByDescending(x => x.FullName),
            ("fullname", _) => query.OrderBy(x => x.FullName),
            ("enrollmentdate", "desc") => query.OrderByDescending(x => x.EnrollmentDate),
            ("enrollmentdate", _) => query.OrderBy(x => x.EnrollmentDate),
            (_, "desc") => query.OrderByDescending(x => x.RegistrationNumber),
            _ => query.OrderBy(x => x.RegistrationNumber)
        };
        var items = await ordered.ThenBy(x => x.Id).Skip(PaginationMetadata.Create(page, pageSize, totalCount).Skip).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(Student student, CancellationToken ct = default) =>
        _dbContext.Students.AddAsync(student, ct).AsTask();
}