using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ProjectManagement.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "New"; // New, In Progress, Completed, On Hold

        // Navigation properties
        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
        public virtual ICollection<ProjectShadowResourceAssignment> ShadowResourceAssignments { get; set; } = new List<ProjectShadowResourceAssignment>();
        public virtual ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
        public virtual ICollection<MonthlySummary> MonthlySummaries { get; set; } = new List<MonthlySummary>();
    }

    public class ProjectComparer : IEqualityComparer<Project>
    {
        public bool Equals(Project? x, Project? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Project obj)
        {
            return obj.Id.GetHashCode();
        }
    }
} 