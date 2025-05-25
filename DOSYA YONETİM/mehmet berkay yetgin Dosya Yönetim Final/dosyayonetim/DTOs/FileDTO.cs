namespace dosyayonetim.DTOs
{
    public class FileDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public long Size { get; set; }
        public int FolderId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class FileUploadDTO
    {
        public int FolderId { get; set; }
    }

    public class FileDownloadDTO
    {
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string Name { get; set; }
    }
}