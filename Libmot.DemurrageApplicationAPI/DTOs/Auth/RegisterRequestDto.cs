using System.ComponentModel.DataAnnotations;

namespace Libmot.DemurrageApplicationAPI.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; //{ Role for: SuperAdmin , FinanceOfficer , Customer ,Driver }

        // Required only when Role = Customer
        public string? CompanyName { get; set; }
        public string? ContactPerson { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
    }
}
