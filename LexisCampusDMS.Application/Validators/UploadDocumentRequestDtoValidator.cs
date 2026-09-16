using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class UploadDocumentRequestDtoValidator : AbstractValidator<UploadDocumentRequestDto>
{
    public UploadDocumentRequestDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título del documento es obligatorio.")
            .MaximumLength(200).WithMessage("El título del documento no puede exceder los 200 caracteres.");

        RuleFor(x => x.StudentRegistration)
            .NotEmpty().WithMessage("La matrícula del estudiante es obligatoria.")
            .MaximumLength(50).WithMessage("La matrícula del estudiante no puede exceder los 50 caracteres.");

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("El tipo de documento seleccionado no es válido.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("El tamaño del archivo debe ser mayor a 0 bytes.");
    }
}
