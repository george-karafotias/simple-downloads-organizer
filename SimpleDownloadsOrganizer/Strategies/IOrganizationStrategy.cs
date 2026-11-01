
using System;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Strategies;

public interface IOrganizationStrategy
{
    string Id { get; }

    string Name { get; }

    string GetDestinationFolder(
        StorageFile file,
        DateTimeOffset modifiedDate);
}
