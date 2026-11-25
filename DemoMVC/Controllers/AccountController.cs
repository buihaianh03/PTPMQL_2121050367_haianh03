using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using DemoMVC.Models;
using DemoMVC.Services;
using DemoMVC.ViewModels;

namespace DemoMVC.Controllers;

public sealed class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<AccountController> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
        _configuration = configuration;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.Identifier.Trim();
        var user = await _userManager.FindByEmailAsync(identifier)
            ?? await _userManager.FindByNameAsync(identifier);
        var result = user is null
            ? null
            : await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

        if (result?.Succeeded == true)
        {
            return await RedirectAfterLoginAsync(model.ReturnUrl, user!);
        }

        ModelState.AddModelError(
            string.Empty,
            result?.IsLockedOut == true
                ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau."
                : result?.IsNotAllowed == true
                    ? "Email chưa được xác thực. Vui lòng xác thực email trước khi đăng nhập."
                : "Thông tin đăng nhập không hợp lệ hoặc email chưa được xác thực.");
        if (result?.IsNotAllowed == true)
        {
            ViewData["EmailNotConfirmed"] = true;
        }

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim()
        };
        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            var cleanupResult = await _userManager.DeleteAsync(user);
            if (!cleanupResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không thể dọn tài khoản sau khi gán role đăng ký thất bại.");
            }

            AddIdentityErrors(roleResult);
            return View(model);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = CreateAccountActionUrl(
            nameof(ConfirmEmail),
            new
            {
                userId = user.Id,
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });

        var emailSent = await TrySendEmailAsync(
            user.Email!,
            "Xác thực tài khoản",
            BuildActionEmail(
                "Xác thực tài khoản của bạn",
                "Xin chào,",
                "Cảm ơn bạn đã đăng ký. Hãy xác thực địa chỉ email để hoàn tất đăng ký và đăng nhập.",
                confirmationUrl,
                "Xác thực tài khoản"));
        if (!emailSent)
        {
            _logger.LogWarning("Verification email delivery failed after account registration.");
        }

        TempData["EmailSent"] = emailSent;
        return RedirectToAction(nameof(RegisterConfirmation));
    }

    [AllowAnonymous]
    public IActionResult RegisterConfirmation() =>
        View(new RegisterConfirmationViewModel
        {
            EmailSent = TempData["EmailSent"] is bool emailSent && emailSent
        });

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        var result = new EmailConfirmationViewModel();
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            result.Message = "Liên kết xác thực không hợp lệ.";
            return View(result);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            result.Message = "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";
            return View(result);
        }

        if (user.EmailConfirmed)
        {
            result.Succeeded = true;
            result.Message = "Email của tài khoản đã được xác thực trước đó.";
            return View(result);
        }

        try
        {
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var confirmation = await _userManager.ConfirmEmailAsync(user, decodedToken);
            result.Succeeded = confirmation.Succeeded;
            result.Message = confirmation.Succeeded
                ? "Xác thực email thành công. Bạn có thể đăng nhập."
                : "Liên kết xác thực không hợp lệ hoặc đã hết hạn.";
        }
        catch (FormatException)
        {
            result.Message = "Liên kết xác thực không hợp lệ.";
        }

        return View(result);
    }

    [AllowAnonymous]
    public IActionResult ResendConfirmationEmail() => View(new EmailViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmationEmail(EmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            await SendConfirmationEmailAsync(user);
        }

        return RedirectToAction(nameof(ResendConfirmationConfirmation));
    }

    [AllowAnonymous]
    public IActionResult ResendConfirmationConfirmation() => View();

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new EmailViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(EmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && user.EmailConfirmed)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = CreateAccountActionUrl(
                nameof(ResetPassword),
                new
                {
                    email = user.Email,
                    token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
                });

            await TrySendEmailAsync(
                user.Email!,
                "Đặt lại mật khẩu",
                BuildActionEmail(
                    "Yêu cầu đặt lại mật khẩu",
                    "Xin chào,",
                    "Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Nếu bạn không yêu cầu, hãy bỏ qua email này.",
                    resetUrl,
                    "Đặt lại mật khẩu"));
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        var model = new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Token = token ?? string.Empty
        };
        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token))
        {
            ModelState.AddModelError(string.Empty, "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        var result = user is null
            ? IdentityResult.Failed(new IdentityError { Code = "InvalidToken" })
            : await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        ModelState.AddModelError(
            string.Empty,
            "Không thể đặt lại mật khẩu. Liên kết có thể đã hết hạn hoặc mật khẩu mới chưa đáp ứng yêu cầu.");
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể đổi mật khẩu. Vui lòng kiểm tra mật khẩu mới và thử lại.");
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["AccountMessage"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<IActionResult> RedirectAfterLoginAsync(string? returnUrl, ApplicationUser user)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl!);
        }

        return await _userManager.IsInRoleAsync(user, "Admin")
            ? RedirectToAction("Index", "StudentRecords")
            : RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationUrl = CreateAccountActionUrl(
            nameof(ConfirmEmail),
            new
            {
                userId = user.Id,
                token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });

        await TrySendEmailAsync(
            user.Email!,
            "Xác thực tài khoản",
            BuildActionEmail(
                "Xác thực tài khoản của bạn",
                "Xin chào,",
                "Hãy xác thực địa chỉ email để hoàn tất đăng ký và đăng nhập.",
                confirmationUrl,
                "Xác thực tài khoản"));
    }

    private async Task<bool> TrySendEmailAsync(string recipient, string subject, string htmlMessage)
    {
        try
        {
            await _emailSender.SendEmailAsync(recipient, subject, htmlMessage);
            return true;
        }
        catch (EmailDeliveryException)
        {
            return false;
        }
    }

    private string CreateAccountActionUrl(string action, object values)
    {
        var publicBaseUrl = _configuration["Application:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return Url.ActionLink(action, "Account", values)
                ?? throw new InvalidOperationException("Không thể tạo liên kết tài khoản.");
        }

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var origin)
            || origin.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(origin.UserInfo)
            || origin.AbsolutePath != "/"
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment))
        {
            _logger.LogError("Application:PublicBaseUrl must be an absolute HTTP(S) origin without path, query, or credentials.");
            throw new InvalidOperationException("Cấu hình địa chỉ công khai của ứng dụng không hợp lệ.");
        }

        return Url.ActionLink(action, "Account", values, origin.Scheme, origin.Authority)
            ?? throw new InvalidOperationException("Không thể tạo liên kết tài khoản.");
    }

    private static string BuildActionEmail(
        string title,
        string greeting,
        string description,
        string actionUrl,
        string actionText)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(actionUrl);
        return $"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;padding:24px;background:#f3f6fa;font-family:Arial,sans-serif;color:#243247">
              <div style="max-width:600px;margin:0 auto;padding:32px;background:#fff;border-radius:12px">
                <h1 style="font-size:22px;color:#0d6efd">{HtmlEncoder.Default.Encode(title)}</h1>
                <p>{HtmlEncoder.Default.Encode(greeting)}</p>
                <p>{HtmlEncoder.Default.Encode(description)}</p>
                <p style="margin:28px 0">
                  <a href="{encodedUrl}" style="display:inline-block;padding:12px 22px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:6px">{HtmlEncoder.Default.Encode(actionText)}</a>
                </p>
                <p style="font-size:13px;color:#637083">Nếu nút không hoạt động, hãy sao chép đường dẫn sau vào trình duyệt:<br><a href="{encodedUrl}">{encodedUrl}</a></p>
                <p style="font-size:13px;color:#637083">Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>
              </div>
            </body>
            </html>
            """;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var message = error.Code switch
            {
                "DuplicateUserName" => "Tên đăng nhập đã được sử dụng.",
                "DuplicateEmail" => "Email đã được sử dụng.",
                "PasswordTooShort" => "Mật khẩu phải có ít nhất 10 ký tự.",
                "PasswordRequiresDigit" => "Mật khẩu cần có ít nhất một chữ số.",
                "PasswordRequiresLower" => "Mật khẩu cần có ít nhất một chữ cái thường.",
                "PasswordRequiresUpper" => "Mật khẩu cần có ít nhất một chữ cái in hoa.",
                _ => "Thông tin đăng ký chưa hợp lệ."
            };
            ModelState.AddModelError(string.Empty, message);
        }
    }
}
