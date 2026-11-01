using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SimpleDownloadsOrganizer.Models;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Services
{
    public sealed class FileMoveService
    {
        public async Task<OrganizationResult> ExecutePlanAsync(
            StorageFolder rootFolder,
            IReadOnlyList<OrganizationPlanItem> plan)
        {
            var result = new OrganizationResult();

            foreach (var item in plan)
            {
                try
                {
                    string sourcePath = Path.Combine(
                        rootFolder.Path,
                        item.FileName);

                    // Nothing to do if the file is already at its destination.
                    if (string.Equals(
                        Path.GetFullPath(sourcePath),
                        Path.GetFullPath(item.DestinationPath),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        result.SkippedFiles.Add(
                            $"{item.FileName}: already in the destination.");
                        continue;
                    }

                    var sourceFile = await rootFolder.GetFileAsync(
                        item.FileName);

                    var destinationFolder =
                        await GetOrCreateDestinationFolderAsync(
                            rootFolder,
                            item.DestinationFolder);

                    // Check for a collision before attempting the move.
                    var existingItem =
                        await destinationFolder.TryGetItemAsync(
                            item.FileName);

                    if (existingItem is not null)
                    {
                        result.SkippedFiles.Add(
                            $"{item.FileName}: destination already exists.");
                        continue;
                    }

                    // FailIfExists is an additional safeguard against
                    // overwriting a file if a collision occurs after the check.
                    await sourceFile.MoveAsync(
                        destinationFolder,
                        item.FileName,
                        NameCollisionOption.FailIfExists);

                    result.MovedFiles.Add(new FileMoveRecord
                    {
                        FileName = item.FileName,
                        OriginalPath = sourcePath,
                        DestinationPath = item.DestinationPath,
                        DestinationFolder = item.DestinationFolder
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        $"{item.FileName}: {ex.Message}");
                }
            }

            return result;
        }

        private static async Task<StorageFolder>
            GetOrCreateDestinationFolderAsync(
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
                currentFolder = await currentFolder.CreateFolderAsync(
                    folderName,
                    CreationCollisionOption.OpenIfExists);
            }

            return currentFolder;
        }
    }
}