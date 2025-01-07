namespace ModpackDownloadAPI
{
    public class FileDownloader(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;
        public static bool Parallel { get; set; } = false;
        public Task<Stream[]> DownloadFiles(IEnumerable<string> downloadURLs)
        {
            if (Parallel) return DownloadParallel(downloadURLs);
            else return DownloadConsistently(downloadURLs);
        }
        public async Task DownloadFiles(IEnumerable<Models.Modrinth.Manifest.File> files)
        {
            if (Parallel)
            {
                var tasks = files.Select(f => f.GetFileStream(this));
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (var file in files)
                {
                    await file.GetFileStream(this);
                }
            }
        }
        private async Task<Stream[]> DownloadParallel(IEnumerable<string> downloadURLs)
        {
            var tasks = downloadURLs.Select(_httpClient.GetByteArrayAsync);
            var filesBytes = await Task.WhenAll(tasks);
            return filesBytes.Select(b => new MemoryStream(b)).ToArray();
        }
        private async Task<Stream[]> DownloadConsistently(IEnumerable<string> downloadURLs)
        {
            var filesBytes = new List<byte[]>();
            foreach (var downloadURL in downloadURLs)
            {
                filesBytes.Add(await _httpClient.GetByteArrayAsync(downloadURL));
            }
            return filesBytes.Select(b => new MemoryStream(b)).ToArray();
        }
        public async Task<Stream> DownloadFile(string downloadURL)
        {
            var filesBytes = await _httpClient.GetByteArrayAsync(downloadURL);
            return new MemoryStream(filesBytes);
        }
    }
}
