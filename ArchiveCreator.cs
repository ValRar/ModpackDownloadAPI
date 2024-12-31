using ModpackDownloadAPI.Models.CurseForge;
using System.IO.Compression;
using System.Web;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ArchiveCreator> _logger;

        public ArchiveCreator(HttpClient httpClient, ILogger<ArchiveCreator> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Stream> CreateArchive(IEnumerable<string> fileDownloadUrls)
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
        public async Task<Stream> CreateArchiveWithReport(IEnumerable<string> fileDownloadUrls, IEnumerable<DownloadUrlErrorReport> reports)
        {
            var downloadedStreams = await DownloadMods(fileDownloadUrls);
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                await AddFiles(archive, downloadedStreams, fileDownloadUrls);
                var reportEntry = archive.CreateEntry("error-report.txt", CompressionLevel.Fastest);
                await WriteErrorReport(reportEntry.Open(), reports);
            }
            stream.Position = 0;
            return stream;
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
        private async Task WriteErrorReport(Stream outStream, IEnumerable<DownloadUrlErrorReport> reports)
        {
            using var writer = new StreamWriter(outStream);
            await writer.WriteAsync("Во время загрузки файлов сборки возникли ошибки, информация о которых представлена в текущем отчете:\nProjectID, FileID, ErrorCode\n");
            foreach (var report in reports)
            {
                await writer.WriteLineAsync($"{report.ProjectID}, {report.FileID}, {report.ErrorCode}");
            }
        }
        private static string GetFileNameFromUrl(string url) => HttpUtility.UrlDecode(url.Split('/').Last());
    }
}
