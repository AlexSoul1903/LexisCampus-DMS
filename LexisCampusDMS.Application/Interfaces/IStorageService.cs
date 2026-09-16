namespace LexisCampusDMS.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(
        Stream fileStream, 
        string storagePath, 
        string contentType, 
        CancellationToken cancellationToken = default);

    Task<Stream> GetFileStreamAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string storagePath, 
        CancellationToken cancellationToken = default);
}
