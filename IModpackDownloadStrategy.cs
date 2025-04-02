namespace ModpackDownloadAPI
{
    public interface IModpackDownloadStrategy
    {
        Task<Stream> DownloadModpackAsync(string modpackDownloadURL);
    }
}
