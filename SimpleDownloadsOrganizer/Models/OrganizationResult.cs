using System.Collections.Generic;

namespace SimpleDownloadsOrganizer.Models
{
    public sealed class OrganizationResult
    {
        public int MovedCount => MovedFiles.Count;

        public int SkippedCount => SkippedFiles.Count;

        public int FailedCount => Errors.Count;

        public List<FileMoveRecord> MovedFiles { get; } = new();

        public List<string> SkippedFiles { get; } = new();

        public List<string> Errors { get; } = new();
    }
}