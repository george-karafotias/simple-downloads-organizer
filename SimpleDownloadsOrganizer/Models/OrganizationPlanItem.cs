using System;

namespace SimpleDownloadsOrganizer.Models
{
    public sealed class OrganizationPlanItem
    {
        public string FileName { get; set; } = string.Empty;

        public DateTimeOffset ModifiedDate { get; set; }

        // Relative to the selected root folder.
        // Example: 2026-10/25
        public string DestinationFolder { get; set; } = string.Empty;

        // Full destination path, used by the preview.
        public string DestinationPath { get; set; } = string.Empty;
    }
}