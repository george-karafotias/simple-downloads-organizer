
using System;
using System.IO;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Strategies;

public sealed class FileExtensionStrategy : IOrganizationStrategy
{
    public string Id => "extension";

    public string Name => "File extension";

    public string GetDestinationFolder(
        StorageFile file,
        DateTimeOffset modifiedDate)
    {
        var extension = Path.GetExtension(file.Name);

        return string.IsNullOrWhiteSpace(extension)
            ? "No Extension"
            : extension.TrimStart('.').ToUpperInvariant();
    }
}
