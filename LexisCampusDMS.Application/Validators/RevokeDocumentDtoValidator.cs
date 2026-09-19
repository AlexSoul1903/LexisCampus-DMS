using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class RevokeDocumentDtoValidator : AbstractValidator<RevokeDocumentDto>
{
    public RevokeDocumentDtoValidator()
    {
        RuleFor(x => x.ResolutionNumber)
            .NotEmpty().WithMessage("El número de resolución es obligatorio para anular el documento.")
            .MaximumLength(100).WithMessage("El número de resolución no puede exceder los 100 caracteres.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo legal de anulación es obligatorio.")
            .MaximumLength(1000).WithMessage("El motivo de anulación no puede exceder los 1000 caracteres.");

        RuleFor(x => x.Observations)
            .MaximumLength(2000).WithMessage("Las observaciones no pueden exceder los 2000 caracteres.");
    }
}
