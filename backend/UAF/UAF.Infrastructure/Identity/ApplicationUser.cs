using Microsoft.AspNetCore.Identity;

namespace UAF.Infrastructure.Identity;

/// <summary>
/// Authentication identity. Academic/student information remains in the Domain model.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
