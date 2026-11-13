using Microsoft.EntityFrameworkCore;
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
        CancellationToken ct = default)
    {
        var query = _dbContext.Students.AsNoTracking();

        if (academicProgramId.HasValue)
            query = query.Where(x => x.AcademicProgramId == academicProgramId.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.RegistrationNumber)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(Student student, CancellationToken ct = default) =>
        _dbContext.Students.AddAsync(student, ct).AsTask();
}