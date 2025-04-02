using ModpackDownloadAPI.Models;
using System.Net;
using System.Web;

namespace ModpackDownloadAPI.Downloaders
{
    public class CurseForgeFileDownloader(HttpClient httpClient)
    {
        public async Task<DownloadResult[]> DownloadFiles(IEnumerable<string> downloadUrls)
        {
            var results = new List<DownloadResult>();
            foreach (var url in downloadUrls)
            {
                results.Add(await DownloadFile(url));
            }
            return results.ToArray();
        }
        private async Task<DownloadResult> DownloadFile(string url)
        {
            using (var response = await httpClient.GetAsync(url))
            {
                if (response.StatusCode != HttpStatusCode.TemporaryRedirect || response.Headers.Location == null)
                    return DownloadResult.Fail();
                url = response.Headers.Location.ToString();
            }
            using (var response = await httpClient.GetAsync(url))
            {
                if (response.StatusCode != HttpStatusCode.Found || response.Headers.Location == null)
                    return DownloadResult.Fail();
                url = response.Headers.Location.ToString();
            }
            using (var response = await httpClient.GetAsync(url))
            {
                var fileName = GetFileNameFromUrl(url);
                var fileStream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                return DownloadResult.Success(fileStream, fileName);
            }
        }
        private static string GetFileNameFromUrl(string url) => HttpUtility.UrlDecode(
            url.Substring(url.LastIndexOf('/') + 1));
    }
}
