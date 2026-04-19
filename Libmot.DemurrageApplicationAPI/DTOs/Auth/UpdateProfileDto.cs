using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Auth
{
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        // Customer-only fields
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
    }
}
