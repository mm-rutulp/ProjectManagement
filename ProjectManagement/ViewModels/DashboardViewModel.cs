using ProjectManagement.Models;

namespace ProjectManagement.ViewModels
{
    public class DashboardViewModel
    {
        // Stats
        public int TotalProjects { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveProjects { get; set; }
        public int TotalWorklogs { get; set; }
        public decimal TotalHours { get; set; }

        // Collections
        public ICollection<Project> RecentProjects { get; set; } = new List<Project>();
        public ICollection<Project> UserProjects { get; set; } = new List<Project>();
        public ICollection<Worklog> RecentWorklogs { get; set; } = new List<Worklog>();
    }
} 