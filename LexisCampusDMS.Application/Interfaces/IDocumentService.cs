using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Interfaces;

public interface IDocumentService
{
    Task<Result<DocumentResponseDto>> UploadDocumentAsync(
        UploadDocumentRequestDto request, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentResponseDto>> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DocumentResponseDto>>> GetByStudentRegistrationAsync(
        string studentRegistration, 
        CancellationToken cancellationToken = default);

    Task<Result<Stream>> DownloadDocumentAsync(
        Guid documentId, 
        int? versionNumber = null, 
        CancellationToken cancellationToken = default);
}
