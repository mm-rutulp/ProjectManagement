using ProjectManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.ViewModels
{
    public class ProjectAssignmentViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Employee")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;
        
        // Added for shadow resource distinction
        public bool IsShadowResource { get; set; } = false;

        public ICollection<ApplicationUser> AvailableEmployees { get; set; } = new List<ApplicationUser>();
        public ICollection<ProjectAssignment> CurrentAssignments { get; set; } = new List<ProjectAssignment>();
        public ICollection<ProjectShadowResourceAssignment> CurrentShadowAssignments { get; set; } = new List<ProjectShadowResourceAssignment>();

        public List<ProjectTeamMemberViewModel> UnifiedTeamMembers { get; set; } = new();

    }
  


    public class ProjectWorklogViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        // For filtering
        public string? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
        public ICollection<ApplicationUser> ProjectMembers { get; set; } = new List<ApplicationUser>();
    }

    public class WorklogCreateViewModel
    {
        public int? WorklogId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Range(0.1, 24)]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Task Type")]
        public string TaskType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        public ICollection<ApplicationUser> AvailableUsers { get; set; } = new List<ApplicationUser>();
    }

    public class MonthlySummaryViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        
        [Required]
        public int Year { get; set; } = DateTime.Now.Year;
        
        [Required]
        [Range(1, 12)]
        public int Month { get; set; } = DateTime.Now.Month;
        
        [Display(Name = "Include Shadow Resources")]
        public bool IncludeShadowResources { get; set; } = true;
        
        public ICollection<MonthlySummary> ExistingSummaries { get; set; } = new List<MonthlySummary>();
    }

    public class ProjectTeamMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsShadow { get; set; }
        public string? AddedByUserId { get; set; }

        public string? AddedByFullName { get; set; }
    }
    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; }
        public List<ProjectTeamMemberViewModel> ShadowTeamMembers { get; set; } = new();
    }



}