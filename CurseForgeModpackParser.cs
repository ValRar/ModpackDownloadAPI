using CurseForge.APIClient.Models;
using ModpackDownloadAPI.Models.CurseForge;
using ModpackDownloadAPI.Models.CurseForge.Manifest;
using System.Text.Json;

namespace ModpackDownloadAPI
{
    public class CurseForgeModpackParser(CurseForge.APIClient.ApiClient curseForgeClient, 
        JsonSerializerOptions serializerOptions, CurseForgeErrorReportFabric errorReportFabric)
    {
        private readonly CurseForge.APIClient.ApiClient _curseForgeClient = curseForgeClient;
        private readonly JsonSerializerOptions _serializerOptions = serializerOptions;
        private readonly CurseForgeErrorReportFabric _errorReportFabric = errorReportFabric;

        // Returns mod download URLS and Error reports
        public async Task<Tuple<string[], CurseForgeErrorReportFabric.Report[]>> ParseManifest(Stream manifestStream)
        {
            var parsedManifest = await JsonSerializer.DeserializeAsync<Manifest>(manifestStream, _serializerOptions);
            var getDownloadUrlTasks = parsedManifest!.Files.Select(f => _curseForgeClient.GetModFileDownloadUrlAsync(f.ProjectID, f.FileID))
                .ToArray();
            var downloadUrlResults = await Task.WhenAll(getDownloadUrlTasks);
            var errorReports = await GenerateErrorReports(parsedManifest.Files, downloadUrlResults);
            return Tuple.Create(downloadUrlResults.Where(r => r.Data != null)
                .Select(r => r.Data)
                .ToArray(), errorReports);
        }
        private async Task<CurseForgeErrorReportFabric.Report[]> GenerateErrorReports(Models.CurseForge.Manifest.File[] files, GenericResponse<string>[] responses)
        {
            var reportTasks = new List<Task<CurseForgeErrorReportFabric.Report>>();
            int i = 0;
            foreach (var response in responses)
            {
                if (response.Error != null)
                {
                    reportTasks.Add(_errorReportFabric.Generate(files[i].ProjectID, files[i].FileID, response.Error.ErrorCode));
                }
                i++;
            }
            var reports = await Task.WhenAll(reportTasks);
            return reports;
        }
    }
}
