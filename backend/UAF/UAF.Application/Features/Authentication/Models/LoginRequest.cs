namespace UAF.Application.Features.Authentication.Models;

public sealed record LoginRequest(string Email, string Password);
