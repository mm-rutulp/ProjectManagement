using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Models;

namespace ProjectManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }
        public DbSet<ProjectShadowResourceAssignment> ProjectShadowResourceAssignments { get; set; }
        public DbSet<Worklog> Worklogs { get; set; }
        public DbSet<MonthlySummary> MonthlySummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships and constraints
            builder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.ProjectAssignments)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.User)
                .WithMany(u => u.ProjectAssignments)
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure shadow resource assignments
            builder.Entity<ProjectShadowResourceAssignment>()
                .HasOne(psa => psa.Project)
                .WithMany(p => p.ShadowResourceAssignments)
                .HasForeignKey(psa => psa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectShadowResourceAssignment>()
                .HasOne(psa => psa.ShadowResource)
                .WithMany(u => u.ShadowResourceAssignments)
                .HasForeignKey(psa => psa.ShadowResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectShadowResourceAssignment>()
                .HasOne(psa => psa.ProjectOnBoardUser)
                .WithMany(u => u.OnBoardedShadowResources)
                .HasForeignKey(psa => psa.ProjectOnBoardUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Worklog>()
                .HasOne(w => w.Project)
                .WithMany(p => p.Worklogs)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Worklog>()
                .HasOne(w => w.User)
                .WithMany(u => u.Worklogs)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure shadow resource in worklogs
            builder.Entity<Worklog>()
                .HasOne(w => w.ShadowResource)
                .WithMany(u => u.ShadowResourceWorklogs)
                .HasForeignKey(w => w.ShadowResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MonthlySummary>()
                .HasOne(ms => ms.Project)
                .WithMany(p => p.MonthlySummaries)
                .HasForeignKey(ms => ms.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 