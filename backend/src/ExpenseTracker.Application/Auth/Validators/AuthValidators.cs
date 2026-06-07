using ExpenseTracker.Application.Auth.Dtos;
using FluentValidation;

namespace ExpenseTracker.Application.Auth.Validators;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("Phone must be in E.164 format, e.g. +14155552671.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("Password must contain a letter.")
            .Matches(@"\d").WithMessage("Password must contain a digit.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
