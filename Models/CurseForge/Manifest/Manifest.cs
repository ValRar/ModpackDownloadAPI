using System.Text.Json.Serialization;

namespace ModpackDownloadAPI.Models.CurseForge.Manifest
{
    public class Manifest
    {
        public string ManifestType { get; set; }
        public int ManifestVersion { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Overrides { get; set; }
        public File[] Files { get; set; }
        public Minecraft Minecraft { get; set; }
    }
}
