using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Models
{
    public class EmployeeRegistration
    {
        
        public int Id { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool IsCreated { get; set; } = false;
    }
}
