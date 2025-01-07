using System.IO.Compression;
using System.Text.Json;
using System.Web;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator
    {
        private readonly CurseForgeModpackParser _modpackParser;
        private readonly ILogger<ArchiveCreator> _logger;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly FileDownloader _fileDownloader;

        public ArchiveCreator(ILogger<ArchiveCreator> logger, CurseForgeModpackParser modpackParser, 
            JsonSerializerOptions serializerOptions, FileDownloader fileDownloader)
        {
            _logger = logger;
            _modpackParser = modpackParser;
            _serializerOptions = serializerOptions;
            _fileDownloader = fileDownloader;
        }
        public async Task<Stream> CreateMrpackArchive(string mrpackDownloadUrl)
        {
            using var mrpackStream = await _fileDownloader.DownloadFile(mrpackDownloadUrl);
            using var mrpackArchive = new ZipArchive(mrpackStream, ZipArchiveMode.Read);
            var memoryStream = new MemoryStream();
            using (var memoryArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await mrpackArchive.ExtractOverridesFolderToAsync(memoryArchive);
                var manifestEntry = mrpackArchive.GetEntry("modrinth.index.json")
                    ?? throw new NullReferenceException("modrinth.index.json not found in mrpack archive.");
                using var manifestStream = manifestEntry.Open();
                var parsedManifest = await JsonSerializer.DeserializeAsync<Models.Modrinth.Manifest.Manifest>(manifestStream, _serializerOptions)
                    ?? throw new NullReferenceException();
                await AddFiles(memoryArchive, parsedManifest.Files);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        public async Task<Stream> CreateCurseForgeArchive(string modpackDownloadUrl)
        {
            using var downloadStream = await _fileDownloader.DownloadFile(modpackDownloadUrl);
            var memoryStream = new MemoryStream();
            using (var fromArchive = new ZipArchive(downloadStream, ZipArchiveMode.Read))
            {
                using var toArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                await fromArchive.ExtractOverridesFolderToAsync(toArchive);
                var manifestEntry = fromArchive.GetEntry("manifest.json") ?? throw new NullReferenceException("Manifest not found in modpack.");
                using var manifestStream = manifestEntry.Open();
                var parsedManifest = await _modpackParser.ParseManifest(manifestStream);
                await AddFiles(toArchive, await _fileDownloader.DownloadFiles(parsedManifest.Item1), parsedManifest.Item1);
                await WriteErrorReport(toArchive, parsedManifest.Item2);
            }
            memoryStream.Position = 0;
            return memoryStream;
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
        private async Task AddFiles(ZipArchive archive, IEnumerable<Models.Modrinth.Manifest.File> files)
        {
            await _fileDownloader.DownloadFiles(files);
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                _logger.LogInformation("Start packaging {}", file.Path);
                await file.FileStream!.CopyToAsync(entryStream);
                _logger.LogInformation("End packaging {}", file.Path);
                file.Dispose();
            }
        }
        private static async Task WriteErrorReport(ZipArchive outArchive, IEnumerable<CurseForgeErrorReportFabric.Report> reports)
        {
            var entry = outArchive.CreateEntry("error-report.txt");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync("Во время загрузки файлов сборки возникли ошибки, информация о которых представлена в текущем отчете:\nDownloadLink, ProjectID, FileID, ErrorCode\n");
            foreach (var report in reports)
            {
                await writer.WriteLineAsync($"{report.DownloadUrl}, {report.ProjectID}, {report.FileID}, {report.ErrorCode}");
            }
        }
        private static string GetFileNameFromUrl(string url) => HttpUtility.UrlDecode(url.Split('/').Last());
    }
}
