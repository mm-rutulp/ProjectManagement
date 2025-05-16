using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Models;

namespace ProjectManagement.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created and migrations are applied
            context.Database.EnsureCreated();

            // Create roles if they don't exist
            string[] roleNames = { "Admin", "Employee" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"Role '{roleName}' created successfully.");
                }
            }

            // Create default admin user if it doesn't exist
            var adminEmail = "admin@projectmanagement.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    Department = "Administration",
                    Position = "System Administrator"
                };

                var adminPassword = "Admin@123";
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine($"Admin user created successfully. Email: {adminEmail}, Password: {adminPassword}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create admin user: {errors}");
                }
            }
            else
            {
                // Make sure we have the admin user object for later use
                adminUser = await userManager.FindByEmailAsync(adminEmail);
            }

            // Create default employee user if it doesn't exist
            var employeeEmail = "employee@projectmanagement.com";
            var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            if (employeeUser == null)
            {
                var employee = new ApplicationUser
                {
                    UserName = employeeEmail,
                    Email = employeeEmail,
                    FullName = "Default Employee",
                    EmailConfirmed = true,
                    Department = "Engineering",
                    Position = "Software Developer"
                };

                var employeePassword = "Employee@123";
                var result = await userManager.CreateAsync(employee, employeePassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(employee, "Employee");
                    Console.WriteLine($"Employee user created successfully. Email: {employeeEmail}, Password: {employeePassword}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create employee user: {errors}");
                }
            }
            else
            {
                // Make sure we have the employee user object for later use
                employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            }
            
            // Create a second default employee user if it doesn't exist
            var employee2Email = "employee2@projectmanagement.com";
            var employee2User = await userManager.FindByEmailAsync(employee2Email);
            if (employee2User == null)
            {
                var employee2 = new ApplicationUser
                {
                    UserName = employee2Email,
                    Email = employee2Email,
                    FullName = "Default Employee 2",
                    EmailConfirmed = true,
                    Department = "Quality Assurance",
                    Position = "QA Engineer"
                };

                var employee2Password = "Employee2@123";
                var result = await userManager.CreateAsync(employee2, employee2Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(employee2, "Employee");
                    Console.WriteLine($"Employee2 user created successfully. Email: {employee2Email}, Password: {employee2Password}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create employee2 user: {errors}");
                }
            }
            else
            {
                // Make sure we have the employee2 user object for later use
                employee2User = await userManager.FindByEmailAsync(employee2Email);
            }

            // Create shadow employee user if it doesn't exist
            var shadowEmail = "shadow@projectmanagement.com";
            var shadowUser = await userManager.FindByEmailAsync(shadowEmail);
            if (shadowUser == null)
            {
                var shadow = new ApplicationUser
                {
                    UserName = shadowEmail,
                    Email = shadowEmail,
                    FullName = "Shadow Employee",
                    EmailConfirmed = true,
                    Department = "External",
                    Position = "Shadow Resource"
                };

                var shadowPassword = "Shadow@123";
                var result = await userManager.CreateAsync(shadow, shadowPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(shadow, "Employee");
                    Console.WriteLine($"Shadow employee created successfully. Email: {shadowEmail}, Password: {shadowPassword}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create shadow employee: {errors}");
                }
            }
            else
            {
                // Make sure we have the shadow user object for later use
                shadowUser = await userManager.FindByEmailAsync(shadowEmail);
            }

            Console.WriteLine("Database initialization completed successfully.");
        }
    }
} 