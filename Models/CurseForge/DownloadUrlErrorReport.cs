namespace ModpackDownloadAPI.Models.CurseForge
{
    public class DownloadUrlErrorReport
    {
        public DownloadUrlErrorReport() { }
        public DownloadUrlErrorReport(int projectID, int fileID, int errorCode)
        {
            ProjectID = projectID;
            FileID = fileID;
            ErrorCode = errorCode;
        }
        public int ProjectID { get; set; }
        public int FileID { get; set; }
        public int ErrorCode { get; set; }
    }
}
