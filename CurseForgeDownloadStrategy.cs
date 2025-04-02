
using System.IO.Compression;
using System.Text.Json;
using ModpackDownloadAPI.Downloaders;

namespace ModpackDownloadAPI
{
    public class CurseForgeDownloadStrategy(FileDownloader fileDownloader,
        CurseForgeFileDownloader cfFileDownloader,
        JsonSerializerOptions serializerOptions,
        ILogger<CurseForgeDownloadStrategy> logger) : IModpackDownloadStrategy
    {
        public async Task<Stream> DownloadModpackAsync(string modpackDownloadURL)
        {
            using var downloadStream = await fileDownloader.DownloadFile(modpackDownloadURL);
            var memoryStream = new MemoryStream();
            using (var fromArchive = new ZipArchive(downloadStream, ZipArchiveMode.Read))
            {
                using var toArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                await fromArchive.ExtractOverridesFolderToAsync(toArchive);

                var manifestEntry = fromArchive.GetEntry("manifest.json") ?? throw new NullReferenceException("Manifest not found in modpack.");
                using var manifestStream = manifestEntry.Open();
                var parsedManifest = JsonSerializer
                    .Deserialize<Models.CurseForge.Manifest.Manifest>(manifestStream, options: serializerOptions)
                    ?? throw new NullReferenceException("Cannot deserialize manifest.");
                var downloadUrls = parsedManifest.Files.Select(f => createDownloadUrl(f.ProjectID, f.FileID));
                
                var downloadResults = await cfFileDownloader.DownloadFiles(downloadUrls);
                foreach (var downloadResult in downloadResults)
                {
                    if (!downloadResult.IsSuccess)
                    {
                        logger.LogWarning("Failed download found!");
                        continue;
                    }
                    using var fileStream = downloadResult.Stream;
                    var fileDirectory = Path.GetExtension(downloadResult.Name) == ".jar" ? 
                        "mods/" : "resourcepacks/";
                    var entry = toArchive.CreateEntry(fileDirectory + downloadResult.Name);
                    logger.LogInformation("Start packaging {}", downloadResult.Name);
                    using var entryStream = entry.Open();
                    await fileStream!.CopyToAsync(entryStream);
                    logger.LogInformation("End packaging {}", downloadResult.Name);
                }
                //var parsedManifest = await modpackParser.ParseManifest(manifestStream);
                //await AddSuccessFetchedFiles(toArchive, await fileDownloader.DownloadFiles(parsedManifest.Item1), parsedManifest.Item1);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        private string createDownloadUrl(int projectID, int fileID) =>
            string.Format("https://www.curseforge.com/api/v1/mods/{0}/files/{1}/download", projectID, fileID);
    }
}
