using Microsoft.AspNetCore.Authorization;

namespace UAF.API.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
