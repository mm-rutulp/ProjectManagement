using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.Models
{
    public class ProjectShadowResourceAssignment
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        // The shadow resource user
        public string ShadowResourceId { get; set; } = string.Empty;
        [ForeignKey("ShadowResourceId")]
        public virtual ApplicationUser? ShadowResource { get; set; }

        // The employee who added this shadow resource - renamed to ProjectOnBoardUser
        public string ProjectOnBoardUserId { get; set; } = string.Empty;
        [ForeignKey("ProjectOnBoardUserId")]
        public virtual ApplicationUser? ProjectOnBoardUser { get; set; }

        [DataType(DataType.Date)]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // External Consultant, Contractor, etc.
    }
} 