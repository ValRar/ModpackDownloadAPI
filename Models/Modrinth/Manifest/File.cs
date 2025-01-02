using System.Text.Json.Serialization;

namespace ModpackDownloadAPI.Models.Modrinth.Manifest
{
    public class File : IDisposable
    {
        [JsonIgnore]
        public Stream? FileStream { get; private set; }
        public async Task GetFileStream(HttpClient client) => FileStream = await client.GetStreamAsync(Downloads[0]);

        public void Dispose()
        {
            FileStream?.Dispose();
        }

        public string Path { get; set; }
        public int FileSize { get; set; }
        public string[] Downloads { get; set; }
        public FileHashes Hashes { get; set; }
        public FileEnv Env { get; set; }
    }
}
