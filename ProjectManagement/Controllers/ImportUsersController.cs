﻿using ClosedXML.Excel;
using Humanizer;
using MagnusMinds.Utility.EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Models;
using ProjectManagement.Services;

namespace ProjectManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ImportUsersController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadUsers(IFormFile file)
        {
            if (file == null || file.Length <= 0)
                return BadRequest("Invalid file");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var row in rows) // Skip header
            {
                var fullName = row.Cell(1).GetValue<string>().Trim();
                var email = row.Cell(2).GetValue<string>().Trim();
                var role = row.Cell(3).GetValue<string>().Trim();

                var userExist = await _userManager.FindByEmailAsync(email);
                if (userExist == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        Department = null,
                        Position = null
                    };
                    var password = GenerateRandomPassword();
                    var result = await _userManager.CreateAsync(user, password); // Default password

                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, role);
                        EmailSender emailSender = new EmailSender(new EmailConfiguration()
                        {
                            From = "mmdevs365@gmail.com",
                            Password = "naoo zxav gvsd lfpe",
                            Port = 465,
                            SmtpServer = "smtp.gmail.com",
                            UserName = "mmdevs365@gmail.com",
                            UseSSL = true,
                            TargetName = null,
                        });
                        string subject = "Welcome to the Project Management System";
                        string htmlContent = $"<p>Dear {fullName},</p><p>Your account has been created successfully.</p><p>Your login details are as follows:</p><p>Email: {email}</p><p>Password: {password}</p><p>To access the portal please go to this URL: https://pms.magnusminds.net/</p>";
                        EmailHelper _email = new EmailHelper(emailSender);

                        List<string> emaillist = new List<string>();
                        emaillist.Add(email);
                        var data = await _email.SendEmail(subject: subject, htmlContent: htmlContent, to: emaillist, cc: null);
                    }
                }
            }

            return Ok("Users imported and emails sent");
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@$?_-";

            var random = new Random();
            var chars = new List<char>
            {
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            string allChars = upper + lower + digits + special;

            while (chars.Count < length)
            {
                chars.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle to ensure random order
            return new string(chars.OrderBy(_ => random.Next()).ToArray());
        }
    }
}
