using UAF.API.Endpoints;
using UAF.API.Validation;
using Xunit;

namespace UAF.Tests.Validation;

public sealed class CreateAdmissionDraftRequestValidatorTests
{
    private readonly CreateAdmissionDraftRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsMissingRequiredFields()
    {
        var request = new CreateAdmissionDraftRequest(
            "", "", "not-an-email", Guid.Empty, default, new string('1', 31));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ApplicationNumber));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ApplicantFullName));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ApplicantEmail));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.AcademicProgramId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.ApplicationDate));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PhoneNumber));
    }

    [Fact]
    public void Validate_AcceptsValidRequest()
    {
        var request = new CreateAdmissionDraftRequest(
            "UAF-2026-0001",
            "Ali Ahsan",
            "applicant@example.com",
            Guid.NewGuid(),
            new DateOnly(2026, 11, 13),
            "+923001234567");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}