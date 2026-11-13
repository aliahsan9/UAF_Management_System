namespace UAF.Application.Features.Authentication.Models;

public sealed record RegisterRequest(string FullName, string Email, string Password);
