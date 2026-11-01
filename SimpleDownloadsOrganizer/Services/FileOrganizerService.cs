
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleDownloadsOrganizer.Models;
using SimpleDownloadsOrganizer.Strategies;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Services;

public sealed class FileOrganizerService
{
    public async Task<List<OrganizationPlanItem>> CreatePlanAsync(
        StorageFolder folder,
        IOrganizationStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(strategy);

        var files = await folder.GetFilesAsync();

        var plan = new List<OrganizationPlanItem>();

        foreach (var file in files)
        {
            var properties = await file.GetBasicPropertiesAsync();
            var modified = properties.DateModified;
            var localModified = modified.LocalDateTime;

            var relativeFolder =
                strategy.GetDestinationFolder(file, modified);

            var destinationPath = Path.Combine(
                folder.Path,
                relativeFolder,
                file.Name);

            plan.Add(new OrganizationPlanItem
            {
                FileName = file.Name,
                ModifiedDate = localModified,
                DestinationFolder = relativeFolder,
                DestinationPath = destinationPath
            });
        }

        return plan
            .OrderByDescending(item => item.ModifiedDate)
            .ToList();
    }
}
