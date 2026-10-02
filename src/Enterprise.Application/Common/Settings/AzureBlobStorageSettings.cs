namespace Enterprise.Application.Common.Settings;

public sealed class AzureBlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "media";
    public string PublicBaseUrl { get; set; } = string.Empty;
}
