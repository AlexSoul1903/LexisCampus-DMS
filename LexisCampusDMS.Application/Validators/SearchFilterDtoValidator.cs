using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class SearchFilterDtoValidator : AbstractValidator<SearchFilterDto>
{
    public SearchFilterDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100 registros.");

        When(x => x.FromDateUtc.HasValue && x.ToDateUtc.HasValue, () =>
        {
            RuleFor(x => x.ToDateUtc)
                .GreaterThanOrEqualTo(x => x.FromDateUtc)
                .WithMessage("La fecha final debe ser posterior o igual a la fecha inicial.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.StudentRegistration), () =>
        {
            RuleFor(x => x.StudentRegistration)
                .MaximumLength(50)
                .WithMessage("La matrícula no puede exceder los 50 caracteres.");
        });

        When(x => x.DocumentType.HasValue, () =>
        {
            RuleFor(x => x.DocumentType!.Value)
                .IsInEnum()
                .WithMessage("El tipo de documento seleccionado no es válido.");
        });
    }
}
