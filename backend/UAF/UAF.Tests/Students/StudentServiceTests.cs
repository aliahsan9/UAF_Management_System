using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Students;
using UAF.Application.Features.Students.Abstractions;
using UAF.Application.Features.Students.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Students;

public sealed class StudentServiceTests
{
    private readonly FakeStudentRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StudentService _service;

    public StudentServiceTests() =>
        _service = new StudentService(_repository, _unitOfWork);

    [Fact]
    public async Task CreateAsync_ValidRequest_NormalizesRegistrationNumberAndPersists()
    {
        var id = await _service.CreateAsync(CreateRequest());

        var student = await _repository.GetByIdAsync(id);
        Assert.NotNull(student);
        Assert.Equal("UAF-2026-CS-001", student.RegistrationNumber);
        Assert.Equal("student@example.com", student.Email);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_DuplicateRegistrationNumber_ThrowsConflict()
    {
        await _service.CreateAsync(CreateRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CreateRequest()));

        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_InactiveAcademicProgram_ThrowsNotFound()
    {
        _repository.ProgramIsActive = false;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateAsync(CreateRequest()));

        Assert.Equal(0, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPaginationMetadata()
    {
        await _service.CreateAsync(CreateRequest());
        await _service.CreateAsync(CreateRequest() with { RegistrationNumber = "UAF-2026-CS-002" });

        var result = await _service.GetPageAsync(1, 1);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task SuspendAndReactivate_ApplyValidLifecycleTransitions()
    {
        var id = await _service.CreateAsync(CreateRequest());

        await _service.SuspendAsync(id);
        Assert.Equal(StudentStatus.Suspended, (await _repository.GetByIdAsync(id))!.Status);

        await _service.ReactivateAsync(id);
        Assert.Equal(StudentStatus.Active, (await _repository.GetByIdAsync(id))!.Status);
        Assert.Equal(3, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ChangeProgramAsync_UpdatesProgramForEligibleStudent()
    {
        var id = await _service.CreateAsync(CreateRequest());
        var newProgramId = Guid.NewGuid();

        await _service.ChangeProgramAsync(id, new ChangeStudentProgramRequest(newProgramId));

        Assert.Equal(newProgramId, (await _repository.GetByIdAsync(id))!.AcademicProgramId);
    }

    [Fact]
    public async Task UpdateProfileAsync_InvalidEmail_DoesNotPartiallyChangeName()
    {
        var id = await _service.CreateAsync(CreateRequest());
        var original = await _repository.GetByIdAsync(id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdateProfileAsync(id,
                new UpdateStudentProfileRequest("Changed Name", "invalid-email", null)));

        var unchanged = await _repository.GetByIdAsync(id);
        Assert.Equal(original!.FullName, unchanged!.FullName);
        Assert.Equal(original.Email, unchanged.Email);
        Assert.Equal(1, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task GraduateAsync_ThenWithdraw_RejectsInvalidTransition()
    {
        var id = await _service.CreateAsync(CreateRequest());

        await _service.GraduateAsync(id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.WithdrawAsync(id));
        Assert.Equal(2, _unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task GetPageAsync_RejectsPageSizeAboveMaximum()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GetPageAsync(1, 101));
    }

    private static CreateStudentRequest CreateRequest() =>
        new(" uaf-2026-cs-001 ", "Test Student", "STUDENT@example.com",
            Guid.NewGuid(), new DateOnly(2026, 11, 13), "03001234567");

    private sealed class FakeStudentRepository : IStudentRepository
    {
        private readonly Dictionary<Guid, Student> _students = new();
        public bool ProgramIsActive { get; set; } = true;

        public Task<bool> RegistrationNumberExistsAsync(
            string registrationNumber, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_students.Values.Any(x =>
                x.RegistrationNumber == registrationNumber &&
                (!excludingId.HasValue || x.Id != excludingId.Value)));
        }

        public Task<bool> AcademicProgramIsActiveAsync(Guid academicProgramId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProgramIsActive);
        }

        public Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPageAsync(
            int page, int pageSize, Guid? academicProgramId, StudentStatus? status,
            string? search, string sortBy, string sortDirection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = _students.Values.AsEnumerable();
            if (academicProgramId.HasValue)
                query = query.Where(x => x.AcademicProgramId == academicProgramId.Value);
            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         x.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         x.RegistrationNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            var ordered = (sortBy, sortDirection) switch
            {
                ("fullname", "desc") => query.OrderByDescending(x => x.FullName),
                ("fullname", _) => query.OrderBy(x => x.FullName),
                ("enrollmentdate", "desc") => query.OrderByDescending(x => x.EnrollmentDate),
                ("enrollmentdate", _) => query.OrderBy(x => x.EnrollmentDate),
                (_, "desc") => query.OrderByDescending(x => x.RegistrationNumber),
                _ => query.OrderBy(x => x.RegistrationNumber)
            };
            var items = ordered.ThenBy(x => x.Id).ToArray();
            return Task.FromResult(((IReadOnlyList<Student>)items.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), items.Length));
        }

        public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _students.TryGetValue(id, out var student);
            return Task.FromResult(student);
        }

        public Task AddAsync(Student student, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _students.Add(student.Id, student);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
    }
}