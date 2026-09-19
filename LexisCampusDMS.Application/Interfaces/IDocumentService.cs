using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Interfaces;

public interface IDocumentService
{
    Task<Result<DocumentResponseDto>> UploadDocumentAsync(
        UploadDocumentRequestDto request, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentResponseDto>> RectifyDocumentAsync(
        RectifyDocumentRequestDto request, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentResponseDto>> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DocumentResponseDto>>> GetByStudentRegistrationAsync(
        string studentRegistration, 
        CancellationToken cancellationToken = default);

    Task<PagedResult<DocumentResponseDto>> SearchAsync(
        SearchFilterDto filter, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentDownloadDto>> DownloadDocumentAsync(
        Guid documentId, 
        int? versionNumber = null, 
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DocumentVersionDto>>> GetVersionsAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentResponseDto>> RevokeDocumentAsync(
        Guid documentId, 
        RevokeDocumentDto request, 
        CancellationToken cancellationToken = default);

    Task<Result<DocumentVerificationResponseDto>> VerifyDocumentAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default);

    Task<Result<StudentDossierDownloadDto>> DownloadStudentDossierZipAsync(
        string studentRegistration, 
        CancellationToken cancellationToken = default);
}
