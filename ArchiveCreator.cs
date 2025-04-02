using System.IO.Compression;
using System.Text.Json;
using System.Web;

namespace ModpackDownloadAPI
{
    public class ArchiveCreator(ModrinthDownloadStrategy modrinthStrategy, 
        CurseForgeDownloadStrategy curseForgeStrategy)
    {
        public async Task<Stream> CreateMrpackArchive(string mrpackDownloadUrl)
        {
            return await modrinthStrategy.DownloadModpackAsync(mrpackDownloadUrl);
        }
        public async Task<Stream> CreateCurseForgeArchive(string modpackDownloadUrl)
        {
            return await curseForgeStrategy.DownloadModpackAsync(modpackDownloadUrl);
        }
    }
}
