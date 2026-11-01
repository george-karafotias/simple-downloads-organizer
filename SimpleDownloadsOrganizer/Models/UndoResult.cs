using System.Collections.Generic;

namespace SimpleDownloadsOrganizer.Models
{
    public sealed class UndoResult
    {
        public int RestoredCount => RestoredFiles.Count;

        public int SkippedCount => SkippedFiles.Count;

        public int FailedCount => Errors.Count;

        public List<FileMoveRecord> RestoredFiles { get; } = new();

        public List<string> SkippedFiles { get; } = new();

        public List<string> Errors { get; } = new();
    }
}