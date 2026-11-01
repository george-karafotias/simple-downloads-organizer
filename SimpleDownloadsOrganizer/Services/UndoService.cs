using SimpleDownloadsOrganizer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Services
{
    public sealed class UndoService
    {
        public async Task<UndoResult> UndoAsync(
            StorageFolder rootFolder,
            IReadOnlyList<FileMoveRecord> moveRecords)
        {
            var result = new UndoResult();

            for (int i = moveRecords.Count - 1; i >= 0; i--)
            {
                var record = moveRecords[i];

                try
                {
                    var destinationFolder =
                        await GetFolderAsync(
                            rootFolder,
                            record.DestinationFolder);

                    if (destinationFolder is null)
                    {
                        result.Errors.Add(
                            $"{record.FileName}: destination folder no longer exists.");
                        continue;
                    }

                    var sourceItem =
                        await destinationFolder.TryGetItemAsync(
                            record.FileName);

                    if (sourceItem is not StorageFile sourceFile)
                    {
                        result.Errors.Add(
                            $"{record.FileName}: moved file could not be found.");
                        continue;
                    }

                    var originalItem =
                        await rootFolder.TryGetItemAsync(
                            record.FileName);

                    if (originalItem is not null)
                    {
                        result.SkippedFiles.Add(
                            $"{record.FileName}: a file already exists at the original location.");
                        continue;
                    }

                    await sourceFile.MoveAsync(
                        rootFolder,
                        record.FileName,
                        NameCollisionOption.FailIfExists);

                    result.RestoredFiles.Add(record);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        $"{record.FileName}: {ex.Message}");
                }
            }

            // Remove empty destination folders after restoring files.
            await RemoveEmptyDestinationFoldersAsync(rootFolder, moveRecords);

            return result;
        }

        private static async Task<StorageFolder?> GetFolderAsync(
            StorageFolder rootFolder,
            string relativePath)
        {
            var currentFolder = rootFolder;

            if (string.IsNullOrWhiteSpace(relativePath))
                return currentFolder;

            string[] folderNames = relativePath.Split(
                new[]
                {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string folderName in folderNames)
            {
                var item = await currentFolder.TryGetItemAsync(folderName);

                if (item is not StorageFolder nextFolder)
                    return null;

                currentFolder = nextFolder;
            }

            return currentFolder;
        }

        private static async Task RemoveEmptyDestinationFoldersAsync(StorageFolder rootFolder, IReadOnlyList<FileMoveRecord> moveRecords)
        {
            var relativeFolders = moveRecords
                .Select(record => record.DestinationFolder)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path.Length)
                .ToList();

            foreach (var relativePath in relativeFolders)
            {
                // Start with the destination folder and work upwards.
                string currentPath = relativePath;

                while (!string.IsNullOrWhiteSpace(currentPath))
                {
                    try
                    {
                        var folder = await GetFolderAsync(
                            rootFolder,
                            currentPath);

                        if (folder is null)
                            break;

                        var files = await folder.GetFilesAsync();
                        var subfolders = await folder.GetFoldersAsync();

                        // Never delete a folder that still contains anything.
                        if (files.Count > 0 || subfolders.Count > 0)
                            break;

                        await folder.DeleteAsync();

                        // Move up to the parent folder.
                        int lastSeparator = currentPath.LastIndexOfAny(
                            new[]
                            {
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                            });

                        if (lastSeparator < 0)
                            break;

                        currentPath = currentPath.Substring(0, lastSeparator);
                    }
                    catch (Exception)
                    {
                        // Cleanup is best-effort. Don't interrupt Undo.
                        break;
                    }
                }
            }
        }
    }
}