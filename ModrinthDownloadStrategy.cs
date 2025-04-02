
using System.IO.Compression;
using System.Text.Json;
using ModpackDownloadAPI.Downloaders;

namespace ModpackDownloadAPI
{
    public class ModrinthDownloadStrategy(FileDownloader fileDownloader, 
        ILogger<ModrinthDownloadStrategy> logger, JsonSerializerOptions serializerOptions) : IModpackDownloadStrategy
    {
        public async Task<Stream> DownloadModpackAsync(string modpackDownloadURL)
        {
            using var mrpackStream = await fileDownloader.DownloadFile(modpackDownloadURL);
            using var mrpackArchive = new ZipArchive(mrpackStream, ZipArchiveMode.Read);
            var memoryStream = new MemoryStream();
            using (var memoryArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await mrpackArchive.ExtractOverridesFolderToAsync(memoryArchive);
                var manifestEntry = mrpackArchive.GetEntry("modrinth.index.json")
                    ?? throw new NullReferenceException("modrinth.index.json not found in mrpack archive.");
                using var manifestStream = manifestEntry.Open();
                var parsedManifest = await JsonSerializer
                    .DeserializeAsync<Models.Modrinth.Manifest.Manifest>(manifestStream, serializerOptions)
                    ?? throw new NullReferenceException();
                await AddFiles(memoryArchive, parsedManifest.Files);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        private async Task AddFiles(ZipArchive archive, IEnumerable<Models.Modrinth.Manifest.File> files)
        {
            await fileDownloader.DownloadFiles(files);
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                logger.LogInformation("Start packaging {}", file.Path);
                await file.FileStream!.CopyToAsync(entryStream);
                logger.LogInformation("End packaging {}", file.Path);
                file.Dispose();
            }
        }
    }
}
