namespace ModpackDownloadAPI.Models.Modrinth.Manifest
{
    public class Manifest
    {
        public int FormatVersion { get; set; }
        public string Game { get; set; }
        public string VersionId { get; set; }
        public string Name { get; set; }
        public File[] Files { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }
    }
}
