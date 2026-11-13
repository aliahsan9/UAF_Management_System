using UAF.Domain.Common;
namespace UAF.Domain.Entities;
public sealed class FeeHead : AuditableEntity
{
    private FeeHead() { Name = Code = string.Empty; }
    public FeeHead(string code, string name, bool isMandatory = true)
    {
        Code = Required(code, 30, nameof(code)).ToUpperInvariant();
        Name = Required(name, 120, nameof(name));
        IsMandatory = isMandatory; IsActive = true;
    }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }
    public void Update(string name, bool isMandatory) { Name = Required(name, 120, nameof(name)); IsMandatory = isMandatory; MarkUpdated(); }
    public void Deactivate() { IsActive = false; MarkUpdated(); }
    private static string Required(string value, int max, string param) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max) throw new ArgumentException($"A value up to {max} characters is required.", param); return value.Trim(); }
}