using FluentValidation;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Core.Domain.Common;

namespace LexisCampusDMS.Application.Validators;

public class ChangeUserRoleRequestDtoValidator : AbstractValidator<ChangeUserRoleRequestDto>
{
    public ChangeUserRoleRequestDtoValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(UserRoles.IsValid).WithMessage("El rol asignado no es válido. Roles permitidos: Admin, Registro, Auditor.");
    }
}
