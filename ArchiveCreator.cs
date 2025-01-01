using ModpackDownloadAPI.Models.CurseForge;
using System.IO.Compression;
using System.Web;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator
    {
        private readonly HttpClient _httpClient;
        private readonly CurseForgeModpackParser _modpackParser;
        private readonly ILogger<ArchiveCreator> _logger;

        public ArchiveCreator(HttpClient httpClient, ILogger<ArchiveCreator> logger, CurseForgeModpackParser modpackParser)
        {
            _httpClient = httpClient;
            _logger = logger;
            _modpackParser = modpackParser;
        }

        public async Task<Stream> CreateModrinthArchive(IEnumerable<string> fileDownloadUrls)
        {
            var downloadedStreams = await DownloadMods(fileDownloadUrls);
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                await AddFiles(archive, downloadedStreams, fileDownloadUrls);
            }
            stream.Position = 0;
            return stream;
        }
        public async Task<Stream> CreateCurseForgeArchive(string modpackDownloadUrl)
        {
            using var downloadStream = await _httpClient.GetStreamAsync(modpackDownloadUrl);
            var memoryStream = new MemoryStream();
            using (var fromArchive = new ZipArchive(downloadStream, ZipArchiveMode.Read))
            {
                using var toArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                foreach (var fromEntry in fromArchive.Entries)
                {
                    if (fromEntry.FullName.StartsWith("overrides/"))
                    {
                        _logger.LogInformation("Start moving {} to root of the archive.", fromEntry.Name);
                        var toEntry = toArchive.CreateEntry(fromEntry.FullName.Substring(10));
                        using var fromStream = fromEntry.Open();
                        using var toStream = toEntry.Open();
                        fromStream.CopyTo(toStream);
                    }
                }
                var manifestEntry = fromArchive.GetEntry("manifest.json") ?? throw new NullReferenceException("Manifest not found in modpack.");
                using var manifestStream = manifestEntry.Open();
                var parsedManifest = await _modpackParser.ParseManifest(manifestStream);
                await AddFiles(toArchive, await DownloadMods(parsedManifest.Item1), parsedManifest.Item1);
                await WriteErrorReport(toArchive, parsedManifest.Item2);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        private async Task<Stream[]> DownloadMods(IEnumerable<string> fileDownloadUrls)
        {
            var downloadTasks = fileDownloadUrls.Select(_httpClient.GetStreamAsync).ToArray();
            return await Task.WhenAll(downloadTasks);
        }
        private async Task AddFiles(ZipArchive archive, Stream[] downloadedFiles, IEnumerable<string> fileUrls)
        {
            int i = 0;
            foreach (var url in fileUrls)
            {
                var fileName = GetFileNameFromUrl(url);
                _logger.LogInformation("Start packaging {}", fileName);
                var directory = Path.GetExtension(fileName) == ".jar" ? "mods/" : "other/";
                var entry = archive.CreateEntry(directory + fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var fileStream = downloadedFiles[i++];
                await fileStream.CopyToAsync(entryStream);
                _logger.LogInformation("End packaging {}", fileName);

            }
        }
        private static async Task WriteErrorReport(ZipArchive outArchive, IEnumerable<DownloadUrlErrorReport> reports)
        {
            var entry = outArchive.CreateEntry("error-report.txt");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync("Во время загрузки файлов сборки возникли ошибки, информация о которых представлена в текущем отчете:\nProjectID, FileID, ErrorCode\n");
            foreach (var report in reports)
            {
                await writer.WriteLineAsync($"{report.ProjectID}, {report.FileID}, {report.ErrorCode}");
            }
        }
        private static string GetFileNameFromUrl(string url) => HttpUtility.UrlDecode(url.Split('/').Last());
    }
}
