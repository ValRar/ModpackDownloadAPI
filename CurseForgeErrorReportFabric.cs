namespace ModpackDownloadAPI
{
    public class CurseForgeErrorReportFabric
    {
        public class Report
        {
            public string? DownloadUrl { get; set; }
            public int ProjectID { get; set; }
            public int FileID { get; set; }
            public int ErrorCode { get; set; }
        }
        public CurseForgeErrorReportFabric(CurseForge.APIClient.ApiClient apiClient)
        {
            _apiClient = apiClient;
        }
        private readonly CurseForge.APIClient.ApiClient _apiClient;
        public async Task<Report> Generate(int projectId, int fileId, int errorCode)
        {
            var modInfo = await _apiClient.GetModAsync(projectId);
            var report = new Report()
            {
                ErrorCode = errorCode,
                FileID = fileId,
                ProjectID = projectId,
                DownloadUrl = modInfo.Data != null ? GenerateDownloadLink(modInfo.Data.Slug, fileId) : null,
            };
            return report;
        }
        private static string GenerateDownloadLink(string slug, int fileId) =>
            string.Format("https://www.curseforge.com/minecraft/mc-mods/{0}/download/{1}", slug, fileId.ToString());
    }
}
