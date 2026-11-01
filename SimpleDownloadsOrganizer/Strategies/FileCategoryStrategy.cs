
using System.IO;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Strategies;

public sealed class FileCategoryStrategy : IOrganizationStrategy
{
    public string Id => "category";

    public string Name => "File category";

    public string GetDestinationFolder(
        StorageFile file,
        System.DateTimeOffset modifiedDate)
    {
        var extension = Path.GetExtension(file.Name)
            .ToLowerInvariant();

        return extension switch
        {
            ".pdf" or ".doc" or ".docx" or ".odt" or ".rtf"
                => "Documents",

            ".txt" or ".md" or ".log"
                => "Text",

            ".csv" or ".json" or ".xml"
                => "Data",

            ".xls" or ".xlsx" or ".ods"
                => "Spreadsheets",

            ".ppt" or ".pptx"
                => "Presentations",

            ".jpg" or ".jpeg" or ".png" or ".gif"
                or ".bmp" or ".webp" or ".svg"
                => "Images",

            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv"
                => "Videos",

            ".mp3" or ".wav" or ".flac" or ".aac"
                => "Audio",

            ".zip" or ".rar" or ".7z" or ".tar" or ".gz"
                => "Archives",

            ".exe" or ".msi" or ".msix"
                => "Installers",

            _ => "Other"
        };
    }
}
