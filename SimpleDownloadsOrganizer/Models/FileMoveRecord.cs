namespace SimpleDownloadsOrganizer.Models
{
    public sealed class FileMoveRecord
    {
        public string FileName { get; set; } = string.Empty;

        public string OriginalPath { get; set; } = string.Empty;

        public string DestinationPath { get; set; } = string.Empty;

        // Relative to the selected root folder.
        public string DestinationFolder { get; set; } = string.Empty;
    }
}