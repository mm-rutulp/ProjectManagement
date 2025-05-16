using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagement.Models
{
    public class MonthlySummary
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        public decimal TotalHours { get; set; }

        [StringLength(1000)]
        public string Summary { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        
        // Flag to indicate if this summary includes shadow resource work
        public bool IncludesShadowWork { get; set; }
    }
} 