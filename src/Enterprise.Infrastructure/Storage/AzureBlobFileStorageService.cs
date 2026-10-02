using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Infrastructure.Storage;

public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly string _publicBaseUrl;
    private readonly ILogger<AzureBlobFileStorageService> _logger;

    public AzureBlobFileStorageService(
        IOptions<AzureBlobStorageSettings> options,
        ILogger<AzureBlobFileStorageService> logger)
    {
        _logger = logger;
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "BlobStorage:ConnectionString is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException(
                "BlobStorage:ContainerName is not configured.");
        }

        _publicBaseUrl = settings.PublicBaseUrl.TrimEnd('/');
        var serviceClient = new BlobServiceClient(settings.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string contentType,
        string folder,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.CreateIfNotExistsAsync(
                PublicAccessType.Blob,
                cancellationToken: cancellationToken);

            var blobName = $"{folder.Trim('/').Trim()}/{fileName.TrimStart('/')}";
            var blob = _container.GetBlobClient(blobName);

            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Blob uploaded folder {Folder} fileName {FileName} contentType {ContentType}",
                folder, fileName, contentType);

            if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
            {
                return $"{_publicBaseUrl}/{_container.Name}/{blobName}";
            }

            return blob.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Blob upload failed folder {Folder} fileName {FileName} contentType {ContentType}",
                folder, fileName, contentType);
            throw;
        }
    }

    public async Task DeleteAsync(string? publicUrlOrBlobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicUrlOrBlobPath))
        {
            return;
        }

        var blobName = TryExtractBlobName(publicUrlOrBlobPath);
        if (blobName is null)
        {
            return;
        }

        try
        {
            var blob = _container.GetBlobClient(blobName);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Blob deleted {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob delete failed for {BlobName}", blobName);
            throw;
        }
    }

    private string? TryExtractBlobName(string publicUrlOrBlobPath)
    {
        if (!Uri.TryCreate(publicUrlOrBlobPath, UriKind.Absolute, out var uri))
        {
            return publicUrlOrBlobPath.TrimStart('/');
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return null;
        }

        // path: /{container}/{blobName...}
        if (string.Equals(segments[0], _container.Name, StringComparison.OrdinalIgnoreCase))
        {
            return string.Join('/', segments.Skip(1));
        }

        return string.Join('/', segments);
    }
}
