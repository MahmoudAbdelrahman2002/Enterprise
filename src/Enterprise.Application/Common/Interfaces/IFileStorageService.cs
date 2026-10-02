namespace Enterprise.Application.Common.Interfaces;

public sealed record ImageUploadFile(
    Stream Content,
    string ContentType,
    string FileName,
    long Length);

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string contentType,
        string folder,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string? publicUrlOrBlobPath, CancellationToken cancellationToken = default);
}
