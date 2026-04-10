using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Auth
{
    public class UpdateProfileDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
