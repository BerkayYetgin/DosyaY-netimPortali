namespace dosyayonetim.DTOs
{
    public class FolderDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? UserTotalSize { get; set; }
    }

    public class FolderCreateDTO
    {
        public string Name { get; set; }
    }

    public class FolderUpdateDTO
    {
        public string Name { get; set; }
    }

    public class FolderWithContentDTO : FolderDTO
    {
        public ICollection<FileDTO> Files { get; set; }
    }
}