using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class UploadDocumentRequestDtoValidator : AbstractValidator<UploadDocumentRequestDto>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private const long MaxFileSizeInBytes = 50 * 1024 * 1024; // 50 MB

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
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .Must(fileName =>
            {
                if (string.IsNullOrWhiteSpace(fileName)) return false;
                var extension = Path.GetExtension(fileName);
                return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage("La extensión del archivo no está permitida. Solo se permiten archivos .pdf, .png, .jpg y .jpeg.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("El tamaño del archivo debe ser mayor a 0 bytes.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("El archivo excede el tamaño máximo permitido de 50 MB.");
    }
}
