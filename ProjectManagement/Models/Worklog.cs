using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.Models
{
    public class Worklog
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Added for shadow resources
        public string? ShadowResourceId { get; set; }
        [ForeignKey("ShadowResourceId")]
        public virtual ApplicationUser? ShadowResource { get; set; }

        // Indicates if this worklog is from a shadow resource
        public bool IsShadowResourceWorklog { get; set; } = false;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.1, 24)]
        public decimal HoursWorked { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TaskType { get; set; } // Development, Testing, Documentation, etc.

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
} 