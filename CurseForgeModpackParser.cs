using CurseForge.APIClient.Models;
using ModpackDownloadAPI.Models.CurseForge;
using ModpackDownloadAPI.Models.CurseForge.Manifest;
using System.Text.Json;

namespace ModpackDownloadAPI
{
    public class CurseForgeModpackParser(CurseForge.APIClient.ApiClient curseForgeClient)
    {
        private readonly CurseForge.APIClient.ApiClient _curseForgeClient = curseForgeClient;

        public async Task<Tuple<string[], DownloadUrlErrorReport[]>> ParseManifest(Stream manifestStream)
        {
            var parsedManifest = await JsonSerializer.DeserializeAsync<Manifest>(manifestStream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var getDownloadUrlTasks = parsedManifest!.Files.Select(f => _curseForgeClient.GetModFileDownloadUrlAsync(f.ProjectID, f.FileID)).ToArray();
            var downloadUrlResults = await Task.WhenAll(getDownloadUrlTasks);
            var errorReports = GenerateErrorReports(parsedManifest.Files, downloadUrlResults);
            return Tuple.Create(downloadUrlResults.Where(r => r.Data != null).Select(r => r.Data).ToArray(), errorReports);
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
