using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.Models
{
    public class ProjectAssignment
    {
        public int Id { get; set; }

        public bool IsDeleted { get; set; } = false;
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // Developer, Tester, Business Analyst, etc.
    }
} 