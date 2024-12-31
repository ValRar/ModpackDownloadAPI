using System.IO.Compression;
using System.Text.Json;
using CurseForge.APIClient.Models;
using ModpackDownloadAPI.Models.CurseForge;
using ModpackDownloadAPI.Models.CurseForge.Manifest;

namespace ModpackDownloadAPI
{
    public class CurseForgeModpackParser
    {
        private readonly HttpClient _httpClient;
        private readonly CurseForge.APIClient.ApiClient _curseForgeClient;
        public CurseForgeModpackParser(HttpClient client, CurseForge.APIClient.ApiClient curseForgeClient)
        {
            _httpClient = client;
            _curseForgeClient = curseForgeClient;
        }
        public async Task<Tuple<string[], DownloadUrlErrorReport[]>> ParseModpack(string downloadUrl)
        {
            using var stream = await _httpClient.GetStreamAsync(downloadUrl);
            using var manifestStream = GetManifest(stream);
            var parsedManifest = await JsonSerializer.DeserializeAsync<Manifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var getDownloadUrlTasks = parsedManifest!.Files.Select(f => _curseForgeClient.GetModFileDownloadUrlAsync(f.ProjectID, f.FileID)).ToArray();
            var downloadUrlResults = await Task.WhenAll(getDownloadUrlTasks);
            var errorReports = GenerateErrorReports(parsedManifest.Files, downloadUrlResults);
            return Tuple.Create(downloadUrlResults.Where(r => r.Data != null).Select(r => r.Data).ToArray(), errorReports);
        }
        private static MemoryStream GetManifest(Stream modpackStream)
        {
            using var zipStream = new ZipArchive(modpackStream, ZipArchiveMode.Read);
            foreach (var entry in zipStream.Entries)
            {
                if (entry.Name != "manifest.json") continue;
                var memoryStream = new MemoryStream();
                using var manifestStream = entry.Open();
                manifestStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            throw new InvalidOperationException("Manifest file not found.");
        }
        private static DownloadUrlErrorReport[] GenerateErrorReports(Models.CurseForge.Manifest.File[] files, GenericResponse<string>[] responses)
        {
            var reports = new List<DownloadUrlErrorReport>();
            int i = 0;
            foreach (var response in responses)
            {
                if (response.Error != null)
                {
                    reports.Add(new DownloadUrlErrorReport(files[i].ProjectID, files[i].FileID, response.Error.ErrorCode));
                }
                i++;
            }
            return reports.ToArray();
        }
    }
}
