using UAF.Domain.Common;

namespace UAF.Domain.Entities;

/// <summary>A schedulable teaching space on a university campus.</summary>
public sealed class TeachingRoom : AuditableEntity
{
    private TeachingRoom() { Code = Name = RoomType = string.Empty; }

    public TeachingRoom(Guid campusId, string code, string name, int capacity, string roomType)
    {
        if (campusId == Guid.Empty) throw new ArgumentException("Campus is required.", nameof(campusId));
        CampusId = campusId;
        Code = Required(code, 30, nameof(code)).ToUpperInvariant();
        Name = Required(name, 120, nameof(name));
        RoomType = Required(roomType, 40, nameof(roomType));
        if (capacity <= 0 || capacity > 5000) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        IsActive = true;
    }

    public Guid CampusId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string RoomType { get; private set; }
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, int capacity, string roomType)
    {
        if (!IsActive) throw new InvalidOperationException("An inactive room cannot be edited.");
        Name = Required(name, 120, nameof(name));
        RoomType = Required(roomType, 40, nameof(roomType));
        if (capacity <= 0 || capacity > 5000) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        MarkUpdated();
    }

    private static string Required(string value, int max, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"A value up to {max} characters is required.", parameter);
        return value.Trim();
    }
}
