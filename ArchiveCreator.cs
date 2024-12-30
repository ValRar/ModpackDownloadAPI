using System.IO.Compression;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator
    {
        private readonly HttpClient _httpClient;

        public ArchiveCreator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MemoryStream> CreateArchive(IEnumerable<string> fileUrls)
        {
            var downloadTasks = fileUrls.Select(_httpClient.GetStreamAsync).ToArray();
            var downloadedStreams = await Task.WhenAll(downloadTasks);
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                int index = 0;
                foreach (var fileUrl in fileUrls)
                {
                    var fileName = fileUrl.Split('/').Last().Replace("%20", " ");
                    var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                    using var entryStream = entry.Open();
                    using var downloadedStream = downloadedStreams[index++];
                    await downloadedStream.CopyToAsync(entryStream);
                }
            }
            stream.Position = 0;
            return stream;
        }
    }
}
