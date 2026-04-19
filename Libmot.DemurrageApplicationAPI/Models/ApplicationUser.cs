using Microsoft.AspNetCore.Identity;

namespace Libmot.DemurrageApplication.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // [ Roles as SuperAdmin, FinanceOfficer, Customer, Driver }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? CustomerProfile { get; set; }
}