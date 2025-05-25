namespace dosyayonetim.DTOs
{
    public class UserStorageDTO
    {
        public long TotalStorage { get; set; }
        public long UsedStorage { get; set; }
        public string UserId { get; set; }
    }

    public class UserStorageUpdateDTO
    {
        public long TotalStorage { get; set; }
    }
}