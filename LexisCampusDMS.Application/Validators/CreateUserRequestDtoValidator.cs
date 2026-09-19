using FluentValidation;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Core.Domain.Common;

namespace LexisCampusDMS.Application.Validators;

public class CreateUserRequestDtoValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo electrónico no puede exceder los 150 caracteres.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .Length(2, 150).WithMessage("El nombre completo debe tener entre 2 y 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(128).WithMessage("La contraseña no puede exceder los 128 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches(@"[0-9]").WithMessage("La contraseña debe contener al menos un número.")
            .Matches(@"[\W_]").WithMessage("La contraseña debe contener al menos un carácter especial.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(UserRoles.IsValid).WithMessage("El rol asignado no es válido. Roles permitidos: Admin, Registro, Auditor.");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("El departamento no puede exceder los 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.StudentRegistration)
            .MaximumLength(50).WithMessage("La matrícula no puede exceder los 50 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.StudentRegistration));
    }
}
