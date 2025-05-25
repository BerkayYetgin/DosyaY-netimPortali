using System.ComponentModel.DataAnnotations;

namespace dosyayonetim.DTOs
{
    public class AssignRoleDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; }
    }
}