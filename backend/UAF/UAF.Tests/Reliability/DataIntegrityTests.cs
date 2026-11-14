using Microsoft.EntityFrameworkCore;
using UAF.Domain.Entities;
using UAF.Infrastructure.Persistence;
using Xunit;

namespace UAF.Tests.Reliability;

public sealed class DataIntegrityTests
{
    [Fact]
    public void AuditableEntity_SoftDeleteAndRestore_PreservesRecordAndTimestamps()
    {
        var student = new Student(
            "UAF-2026-CS-001", "Test Student", "student@example.com",
            Guid.NewGuid(), new DateOnly(2026, 9, 1));

        student.SoftDelete();

        Assert.True(student.IsDeleted);
        Assert.NotNull(student.DeletedAtUtc);
        Assert.NotNull(student.UpdatedAtUtc);

        student.Restore();

        Assert.False(student.IsDeleted);
        Assert.Null(student.DeletedAtUtc);
        Assert.NotNull(student.UpdatedAtUtc);
    }

    [Fact]
    public void IdempotencyRecord_StoresCompletionAndReplayMetadata()
    {
        var now = DateTimeOffset.UtcNow;
        var record = new IdempotencyRecord(
            Guid.NewGuid().ToString(), "request-001", new string('A', 64), now.AddHours(24));

        record.Complete(201, "application/json", "{\"id\":\"created\"}", replayable: true);

        Assert.True(record.IsCompleted);
        Assert.True(record.IsReplayable);
        Assert.Equal(201, record.ResponseStatusCode);
        Assert.Equal("application/json", record.ResponseContentType);
        Assert.Equal("{\"id\":\"created\"}", record.ResponseBody);
    }

    [Fact]
    public void IdempotencyRecord_RejectsRecompletion()
    {
        var now = DateTimeOffset.UtcNow;
        var record = new IdempotencyRecord(
            Guid.NewGuid().ToString(), "request-002", new string('B', 64), now.AddHours(24));
        record.Complete(200, "application/json", "{}", replayable: true);

        Assert.Throws<InvalidOperationException>(() =>
            record.Complete(200, "application/json", "{}", replayable: true));
    }

    [Fact]
    public void UafModel_AppliesSoftDeleteFilterAndRowVersionToAuditableEntities()
    {
        var options = new DbContextOptionsBuilder<UafDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=UafModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new UafDbContext(options);

        var student = db.Model.FindEntityType(typeof(Student));
        Assert.NotNull(student);
        Assert.NotNull(student!.GetQueryFilter());

        var rowVersion = student.FindProperty(nameof(Student.RowVersion));
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion!.IsConcurrencyToken);
        Assert.True(rowVersion.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }
}
