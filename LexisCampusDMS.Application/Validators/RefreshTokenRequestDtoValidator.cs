using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El token de actualización (refresh token) es obligatorio.");
    }
}
