using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dosyayonetim.Models
{
    public class UserStorage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        
        [Required]
        public long TotalStorage { get; set; } // Total storage in bytes (5GB = 5 * 1024 * 1024 * 1024 bytes)
        
        [Required]
        public long UsedStorage { get; set; } // Used storage in bytes
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
} 