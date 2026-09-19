using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class UpdateUserRequestDtoValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .Length(2, 150).WithMessage("El nombre completo debe tener entre 2 y 150 caracteres.");

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("El departamento no puede exceder los 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.StudentRegistration)
            .MaximumLength(50).WithMessage("La matrícula no puede exceder los 50 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.StudentRegistration));
    }
}
