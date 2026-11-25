using Microsoft.AspNetCore.Identity;
using DemoMVC.Models;

namespace DemoMVC.Services;

public static class IdentitySeed
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"Tạo role {roleName}");
            }
        }

        var username = configuration["AdminSeed:Username"];
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];
        var hasAnyAdminSetting = !string.IsNullOrWhiteSpace(username)
            || !string.IsNullOrWhiteSpace(email)
            || !string.IsNullOrWhiteSpace(password);
        if (!hasAnyAdminSetting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Cần cấu hình đầy đủ AdminSeed:Username, AdminSeed:Email và AdminSeed:Password.");
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByNameAsync(username);
        var emailOwner = await userManager.FindByEmailAsync(email);
        if (admin is not null && emailOwner is not null && admin.Id != emailOwner.Id)
        {
            throw new InvalidOperationException("Username và email AdminSeed đang thuộc hai tài khoản khác nhau.");
        }

        admin ??= emailOwner;
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, password), "Tạo tài khoản Admin");
        }
        else
        {
            if (!string.Equals(admin.UserName, username, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(admin.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Tài khoản AdminSeed đã tồn tại nhưng username/email không khớp cấu hình.");
            }

            if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true;
                EnsureSucceeded(await userManager.UpdateAsync(admin), "Xác thực email tài khoản Admin");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, "Admin"), "Gán role Admin");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"{operation} thất bại: {errors}");
        }
    }
}
