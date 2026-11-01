
using System;
using System.Globalization;
using System.IO;
using Windows.Storage;

namespace SimpleDownloadsOrganizer.Strategies;

public sealed class MonthAndDayStrategy : IOrganizationStrategy
{
    public string Id => "month-day";

    public string Name => "Month and day";

    public string GetDestinationFolder(
        StorageFile file,
        DateTimeOffset modifiedDate)
    {
        var localDate = modifiedDate.LocalDateTime;

        var month = localDate.ToString(
            "yyyy-MM",
            CultureInfo.InvariantCulture);

        var day = localDate.ToString(
            "dd",
            CultureInfo.InvariantCulture);

        return Path.Combine(month, day);
    }
}
