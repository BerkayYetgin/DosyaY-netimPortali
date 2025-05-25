using Microsoft.AspNetCore.Identity;

namespace dosyayonetim.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public UserStorage Storage { get; set; }
        public ICollection<Folder> Folders { get; set; }
        public ICollection<File> Files { get; set; }
    }
} 