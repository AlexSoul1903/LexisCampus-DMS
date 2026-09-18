using FluentValidation;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Validators;

public class RectifyDocumentRequestDtoValidator : AbstractValidator<RectifyDocumentRequestDto>
{
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private const long MaxFileSizeInBytes = 50 * 1024 * 1024; // 50 MB

    public RectifyDocumentRequestDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.DocumentId.HasValue && x.DocumentId.Value != Guid.Empty) ||
                       (!string.IsNullOrWhiteSpace(x.StudentRegistration) && x.DocumentType.HasValue))
            .WithMessage("Debe proporcionar un ID de documento válido o la combinación de matrícula y tipo de documento.");

        RuleFor(x => x.ChangeReason)
            .NotEmpty().WithMessage("El motivo de la rectificación es obligatorio.")
            .MinimumLength(5).WithMessage("El motivo de la rectificación debe contener al menos 5 caracteres.")
            .MaximumLength(500).WithMessage("El motivo de la rectificación no puede exceder los 500 caracteres.");

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
