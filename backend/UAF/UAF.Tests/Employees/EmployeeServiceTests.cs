using UAF.Application.Common.Abstractions;
using UAF.Application.Features.Employees;
using UAF.Application.Features.Employees.Abstractions;
using UAF.Application.Features.Employees.Models;
using UAF.Domain.Entities;
using UAF.Domain.Enums;
using Xunit;

namespace UAF.Tests.Employees;

public sealed class EmployeeServiceTests
{
    private readonly FakeRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly EmployeeService _service;

    public EmployeeServiceTests() => _service = new EmployeeService(_repository, _unitOfWork);

    [Fact]
    public async Task CreateAsync_NormalizesEmployeeNumberAndPersists()
    {
        var id = await _service.CreateAsync(Request());
        var employee = await _repository.GetByIdAsync(id, default);
        Assert.NotNull(employee);
        Assert.Equal("EMP-001", employee.EmployeeNumber);
        Assert.Equal("ali@example.edu", employee.Email);
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_RejectsInactiveDepartmentWithoutSaving()
    {
        _repository.DepartmentActive = false;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(Request()));
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmployeeNumber()
    {
        await _service.CreateAsync(Request());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(Request()));
        Assert.Equal(1, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_ValidatesIdentityExistenceAndSingleLink()
    {
        var identity = Guid.NewGuid();
        _repository.IdentityExists = false;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(Request() with { IdentityUserId = identity }));
        _repository.IdentityExists = true;
        _repository.IdentityLinked = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(Request() with { IdentityUserId = identity }));
        Assert.Equal(0, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task GetPageAsync_ValidatesPaginationAndMapsTotalPages()
    {
        await _service.CreateAsync(Request());
        await _service.CreateAsync(Request() with { EmployeeNumber = "EMP-002" });
        var page = await _service.GetPageAsync(1, 1, null, null, null, default);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetPageAsync(0, 10, null, null, null, default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetPageAsync(1, 101, null, null, null, default));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetPageAsync(1, 10, Guid.Empty, null, null, default));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownEmployee_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), default));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetByIdAsync(Guid.Empty, default));
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesEmployeeAndPersists()
    {
        var id = await _service.CreateAsync(Request());
        await _service.UpdateProfileAsync(id, new("Ali Updated", "updated@example.edu", "Senior Lecturer", "03000000000"), default);
        var employee = await _repository.GetByIdAsync(id, default);
        Assert.Equal("Ali Updated", employee!.FullName);
        Assert.Equal("updated@example.edu", employee.Email);
        Assert.Equal(2, _unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task TransferAsync_RequiresActiveDepartmentAndExistingEmployee()
    {
        var id = await _service.CreateAsync(Request());
        _repository.DepartmentActive = false;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.TransferAsync(id, new(Guid.NewGuid()), default));
        _repository.DepartmentActive = true;
        var newDepartment = Guid.NewGuid();
        await _service.TransferAsync(id, new(newDepartment), default);
        Assert.Equal(newDepartment, (await _repository.GetByIdAsync(id, default))!.DepartmentId);
    }

    [Fact]
    public async Task ChangeStatusAsync_TerminatesEmployeeAndPreventsReactivation()
    {
        var id = await _service.CreateAsync(Request());
        await _service.ChangeStatusAsync(id, new(EmployeeStatus.Terminated, new DateOnly(2026, 11, 1)), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangeStatusAsync(id, new(EmployeeStatus.Active, null), default));
        Assert.Equal(EmployeeStatus.Terminated, (await _repository.GetByIdAsync(id, default))!.Status);
    }

    [Fact]
    public async Task CreateTeacherProfileAsync_RequiresAcademicEmployeeAndPreventsDuplicates()
    {
        var adminId = await _service.CreateAsync(Request() with { EmployeeNumber = "EMP-ADMIN", Category = EmployeeCategory.Administrative });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateTeacherProfileAsync(adminId, TeacherRequest(), default));

        var teacherId = await _service.CreateAsync(Request());
        var profile = await _service.CreateTeacherProfileAsync(teacherId, TeacherRequest(), default);
        Assert.Equal(teacherId, profile.EmployeeId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateTeacherProfileAsync(teacherId, TeacherRequest(), default));
    }

    [Fact]
    public async Task UpdateTeacherProfileAsync_RequiresExistingProfile()
    {
        var id = await _service.CreateAsync(Request());
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateTeacherProfileAsync(id, new("PhD", "AI", "Professor"), default));
        await _service.CreateTeacherProfileAsync(id, TeacherRequest(), default);
        var updated = await _service.UpdateTeacherProfileAsync(id, new("PhD Computer Science", "Machine Learning", "Associate Professor"), default);
        Assert.Equal("Machine Learning", updated.Specialization);
    }

    private CreateEmployeeRequest Request() => new(
        " emp-001 ", "Ali Example", "ALI@EXAMPLE.EDU", "Lecturer",
        _repository.DepartmentId, EmployeeCategory.Academic, new DateOnly(2024, 1, 1), "03001234567", null);

    private static CreateTeacherProfileRequest TeacherRequest() =>
        new("MS Computer Science", "Artificial Intelligence", "Lecturer");

    private sealed class FakeRepository : IEmployeeRepository
    {
        private readonly Dictionary<Guid, Employee> _employees = new();
        private readonly Dictionary<Guid, TeacherProfile> _profiles = new();
        public Guid DepartmentId { get; } = Guid.NewGuid();
        public bool DepartmentActive { get; set; } = true;
        public bool IdentityExists { get; set; } = true;
        public bool IdentityLinked { get; set; }

        public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(DepartmentActive && id != Guid.Empty);
        public Task<bool> EmployeeNumberExistsAsync(string number, Guid? excludingId, CancellationToken ct) =>
            Task.FromResult(_employees.Values.Any(x => x.EmployeeNumber == number && x.Id != excludingId));
        public Task<bool> IdentityExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(IdentityExists);
        public Task<bool> IdentityAlreadyLinkedAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(IdentityLinked || _employees.Values.Any(x => x.IdentityUserId == id));
        public Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPageAsync(
            int page, int pageSize, Guid? departmentId, EmployeeStatus? status, EmployeeCategory? category, CancellationToken ct)
        {
            IEnumerable<Employee> query = _employees.Values;
            if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
            if (status.HasValue) query = query.Where(x => x.Status == status);
            if (category.HasValue) query = query.Where(x => x.Category == category);
            var all = query.OrderBy(x => x.EmployeeNumber).ToArray();
            return Task.FromResult(((IReadOnlyList<Employee>)all.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), all.Length));
        }
        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            _employees.TryGetValue(id, out var employee);
            return Task.FromResult(employee);
        }
        public Task AddAsync(Employee employee, CancellationToken ct)
        {
            _employees.Add(employee.Id, employee);
            return Task.CompletedTask;
        }
        public Task<TeacherProfile?> GetTeacherProfileAsync(Guid id, CancellationToken ct)
        {
            _profiles.TryGetValue(id, out var profile);
            return Task.FromResult(profile);
        }
        public Task AddTeacherProfileAsync(TeacherProfile profile, CancellationToken ct)
        {
            _profiles.Add(profile.EmployeeId, profile);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
