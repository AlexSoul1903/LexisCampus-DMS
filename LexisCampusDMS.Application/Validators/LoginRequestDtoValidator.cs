using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario o correo electrónico es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre de usuario o correo no puede exceder los 100 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .MaximumLength(128).WithMessage("La contraseña no puede exceder los 128 caracteres.");
    }
}
