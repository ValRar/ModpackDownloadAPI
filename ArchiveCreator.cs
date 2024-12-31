using ModpackDownloadAPI.Models.CurseForge;
using System.IO.Compression;
using System.Web;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator
    {
        private readonly HttpClient _httpClient;

        public ArchiveCreator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Stream> CreateArchive(IEnumerable<string> fileDownloadUrls)
        {
            var downloadedStreams = await DownloadMods(fileDownloadUrls);
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddFiles(archive, downloadedStreams, fileDownloadUrls);
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
                AddFiles(archive, downloadedStreams, fileDownloadUrls);
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
        private static void AddFiles(ZipArchive archive, Stream[] downloadedFiles, IEnumerable<string> fileUrls)
        {
            int i = 0;
            foreach (var url in fileUrls)
            {
                var fileName = GetFileNameFromUrl(url);
                var directory = Path.GetExtension(fileName) == ".jar" ? "mods/" : "other/";
                var entry = archive.CreateEntry(directory + fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var fileStream = downloadedFiles[i++];
                fileStream.CopyTo(entryStream);
            }
        }
        private static async Task WriteErrorReport(Stream outStream, IEnumerable<DownloadUrlErrorReport> reports)
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
