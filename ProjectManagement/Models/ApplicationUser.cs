using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? Position { get; set; }

        public bool IsDeleted { get; set; } = false;


        // Navigation properties
        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
        
        // Shadow resource assignments where this user is the shadow resource
        public virtual ICollection<ProjectShadowResourceAssignment> ShadowResourceAssignments { get; set; } = new List<ProjectShadowResourceAssignment>();
        
        // Shadow resource assignments added by this user
        public virtual ICollection<ProjectShadowResourceAssignment> OnBoardedShadowResources { get; set; } = new List<ProjectShadowResourceAssignment>();
        
        public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
        
        // Worklogs created as a shadow resource
        public virtual ICollection<Worklog> ShadowResourceWorklogs { get; set; } = new List<Worklog>();
    }
} 