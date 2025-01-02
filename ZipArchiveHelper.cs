using System.IO.Compression;

namespace ModpackDownloadAPI
{
    public static class ZipArchiveHelper
    {
        public static async Task ExtractOverridesFolderToAsync(this ZipArchive from, ZipArchive to)
        {
            foreach (ZipArchiveEntry fromEntry in from.Entries)
            {
                if (fromEntry.FullName.StartsWith("overrides/") && fromEntry.FullName.Length > 10)
                {
                    using var fromStream = fromEntry.Open();
                    var toEntry = to.CreateEntry(fromEntry.FullName.Substring(10));
                    using var toStream = toEntry.Open();
                    await fromStream.CopyToAsync(toStream);
                }
            }
        }
    }
}
