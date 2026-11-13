using FluentValidation;
using UAF.API.Endpoints;

namespace UAF.API.Validation;

public sealed class CreateAdmissionDraftRequestValidator : AbstractValidator<CreateAdmissionDraftRequest>
{
    public CreateAdmissionDraftRequestValidator()
    {
        RuleFor(request => request.ApplicationNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.ApplicantFullName)
            .NotEmpty()
            .MaximumLength(180);

        RuleFor(request => request.ApplicantEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(request => request.AcademicProgramId)
            .NotEmpty();

        RuleFor(request => request.ApplicationDate)
            .Must(date => date != default)
            .WithMessage("Application date is required.");

        RuleFor(request => request.PhoneNumber)
            .MaximumLength(30);
    }
}