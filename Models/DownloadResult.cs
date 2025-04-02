namespace ModpackDownloadAPI.Models
{
    public class DownloadResult
    {
        public static DownloadResult Fail()
        {
            return new DownloadResult
            {
                IsSuccess = false,
            };
        }
        public static DownloadResult Success(Stream stream, string name)
        {
            return new DownloadResult
            {
                IsSuccess = true,
                Stream = stream,
                Name = name
            };
        }
        public bool IsSuccess { get; set; }
        public string Name { get; set; }
        public Stream? Stream { get; set; }
    }
}
