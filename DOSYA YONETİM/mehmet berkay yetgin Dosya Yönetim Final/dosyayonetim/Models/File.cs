using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dosyayonetim.Models
{
    public class File
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        [Required]
        public string FilePath { get; set; } // Physical path of the file
        
        [Required]
        public string FileType { get; set; } // MIME type
        
        [Required]
        public long Size { get; set; } // File size in bytes
        
        [Required]
        public int FolderId { get; set; }
        
        [ForeignKey("FolderId")]
        public Folder Folder { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
} 