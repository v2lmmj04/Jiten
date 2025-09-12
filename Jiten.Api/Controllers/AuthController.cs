using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Google.Apis.Auth;
using Jiten.Api.Dtos;
using Jiten.Api.Dtos.Requests;
using Jiten.Api.Services;
using Jiten.Core;
using Jiten.Core.Data.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly UserDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UrlEncoder _urlEncoder;
    private readonly IMemoryCache _memoryCache;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        IEmailSender emailSender,
        UserDbContext context,
        IConfiguration configuration,
        UrlEncoder urlEncoder,
        IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _context = context;
        _configuration = configuration;
        _urlEncoder = urlEncoder;
        _memoryCache = memoryCache;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!await ValidateRecaptcha(model.RecaptchaResponse))
            BadRequest(new { message = "Recaptcha verification failed." });

        if (!model.TosAccepted)
            return BadRequest(new { message = "You must accept the terms of service to register." });

        var userName = model.Username.Trim();
        var email = model.Email.Trim();

        var userExists = await _userManager.FindByNameAsync(userName);
        if (userExists != null) return Conflict(new { message = "Username already exists." });

        var emailExists = await _userManager.FindByEmailAsync(email);
        if (emailExists != null) return Conflict(new { message = "Email already registered." });


        if (userName.Length is < 2 or > 20)
            return BadRequest(new { message = "Username must be between 2 and 20 characters." });

        var user = new User
                   {
                       UserName = userName, Email = email, SecurityStamp = Guid.NewGuid().ToString(), TosAcceptedAt = DateTime.UtcNow,
                       ReceivesNewsletter = model.ReceiveNewsletter
                   };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new
                              {
                                  message =
                                      "User creation failed. Make sure your password is long enough and contains lower case, upper case and digits.",
                                  errors = result.Errors.Select(e => e.Description)
                              });
        }

        await _userManager.AddToRoleAsync(user, nameof(UserRole.User));

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var url = "https://jiten.moe";
        var callbackUrl = $"{url}/confirm-email?userId={user.Id}&code={code}";

        await _emailSender.SendEmailAsync(email, "Jiten - Confirm your email",
                                          $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>." +
                                          $"<br/>Do not share this link with anyone.<br/>If you did not request an account creation, please ignore this email.");

        return Ok(new { message = "Registration successful. Please check your email to confirm your account." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return BadRequest("User ID and code are required.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return BadRequest("Invalid token format.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
            return Ok(new { message = "Email confirmed successfully. You can now log in." });

        return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByNameAsync(model.UsernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(model.UsernameOrEmail);

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Unauthorized(new { message = "Account locked due to too many failed login attempts. Please try again later." });

        if (result.IsNotAllowed)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new { message = "Email not confirmed. Please check your email or resend confirmation." });
            }

            return Unauthorized(new { message = "Login not allowed for an unknown reason." });
        }

        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var tokens = await _tokenService.GenerateTokens(user);
        await _context.SaveChangesAsync(); // Save refresh token
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null) return BadRequest(new { message = "Invalid access token or refresh token." });

        var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return BadRequest(new { message = "Invalid token claims." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return BadRequest(new { message = "User not found." });

        var oldRefreshToken = await _context.RefreshTokens
                                            .FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken && rt.UserId == userId);

        if (oldRefreshToken == null || oldRefreshToken.IsUsed || oldRefreshToken.IsRevoked || oldRefreshToken.ExpiryDate < DateTime.UtcNow)
        {
            if (oldRefreshToken != null && (oldRefreshToken.IsUsed || oldRefreshToken.IsRevoked))
            {
                _context.RefreshTokens.Remove(oldRefreshToken); // Clean up if already invalid
                await _context.SaveChangesAsync();
            }

            return BadRequest(new { message = "Invalid or expired refresh token." });
        }

        oldRefreshToken.IsUsed = true;
        _context.RefreshTokens.Update(oldRefreshToken);

        var newTokens = await _tokenService.GenerateTokens(user);

        await _context.SaveChangesAsync(); // Saves both the used old token and the new token

        return Ok(newTokens);
    }


    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!await ValidateRecaptcha(model.RecaptchaResponse))
            BadRequest(new { message = "Recaptcha verification failed." });

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user) || user.PasswordHash == null)
        {
            return Ok(new
                      {
                          message =
                              "If your email address is registered and confirmed, you will receive a password reset link. If you created your account with google auth, you will need to connect through google."
                      });
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var url = "https://jiten.moe";
        var callbackUrl = $"{url}/reset-password?email={_urlEncoder.Encode(user.Email)}&code={code}";

        await _emailSender.SendEmailAsync(model.Email, "Jiten - Reset Your Password",
                                          $"Please reset your password on Jiten.moe by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.<br/>Do not share this link with anyone.<br/>If you did not request a password reset, please ignore this email.");

        return Ok(new
                  {
                      message =
                          "If your email address is registered and confirmed, you will receive a password reset link. If you created your account with google auth, you will need to connect through google."
                  });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return BadRequest(new { message = "Password reset failed. Please try again or request a new link." });
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid token format. Password reset failed." });
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
        if (result.Succeeded)
        {
            // Optionally, revoke all refresh tokens for the user upon password reset for security
            var userRefreshTokens = await _context.RefreshTokens
                                                  .Where(rt => rt.UserId == user.Id && !rt.IsRevoked && !rt.IsUsed)
                                                  .ToListAsync();
            foreach (var rt in userRefreshTokens) rt.IsRevoked = true;
            _context.RefreshTokens.UpdateRange(userRefreshTokens);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }

        return BadRequest(new { message = "Password reset failed.", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var userRefreshTokens = await _context.RefreshTokens
                                              .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed)
                                              .ToListAsync();

        if (!userRefreshTokens.Any())
        {
            return Ok(new { message = "No active refresh tokens to revoke." });
        }

        foreach (var token in userRefreshTokens)
        {
            token.IsRevoked = true;
        }

        _context.RefreshTokens.UpdateRange(userRefreshTokens);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Refresh tokens revoked." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new { user.Id, user.UserName, user.Email, user.EmailConfirmed, Roles = roles });
    }

    [HttpPost("signin-google")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> GoogleSignIn([FromBody] GoogleLoginRequest loginRequest)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.IdToken))
        {
            return BadRequest(new { message = "Missing idToken" });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(loginRequest.IdToken,
                                                                 new GoogleJsonWebSignature.ValidationSettings
                                                                 {
                                                                     Audience = [_configuration["Google:ClientId"]]
                                                                 });
        }
        catch (InvalidJwtException ex)
        {
            return Unauthorized(new { message = "Invalid Google ID token", detail = ex.Message });
        }

        if (!payload.EmailVerified)
        {
            return Unauthorized(new { message = "Google email not verified" });
        }

        var email = payload.Email;
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user != null)
        {
            // User already exists, proceed with normal login
            var tokens = await _tokenService.GenerateTokens(user);
            await _context.SaveChangesAsync(); // Save refresh token
            return Ok(tokens);
        }

        // New user - generate temporary token for registration flow
        var tempToken = Guid.NewGuid().ToString();
        var registrationData = new GoogleRegistrationData
                               {
                                   Email = email, Name = payload.Name, Picture = payload.Picture, TempToken = tempToken
                               };

        // Store temp data in cache (you could also use a database table)
        _memoryCache.Set($"google_registration_{tempToken}", registrationData, TimeSpan.FromMinutes(15));

        return Ok(new
                  {
                      requiresRegistration = true, tempToken = tempToken, email = email, name = payload.Name, picture = payload.Picture
                  });
    }

    [HttpPost("complete-google-registration")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> CompleteGoogleRegistration([FromBody] CompleteGoogleRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TempToken))
        {
            return BadRequest(new { message = "Missing temporary token" });
        }

        if (!_memoryCache.TryGetValue($"google_registration_{request.TempToken}", out GoogleRegistrationData registrationData))
        {
            return BadRequest(new { message = "Invalid or expired registration token" });
        }

        if (!request.TosAccepted)
        {
            return BadRequest(new { message = "Terms of service must be accepted" });
        }

        var username = request.Username.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        if (username.Length < 3 || username.Length > 20)
        {
            return BadRequest(new { message = "Username must be between 3 and 20 characters" });
        }

        var userExists = await _userManager.FindByNameAsync(username);
        if (userExists != null) return Conflict(new { message = "Username already exists." });

        var usernameExists = await _userManager.Users.AnyAsync(u => u.UserName == request.Username);
        if (usernameExists)
        {
            return BadRequest(new { message = "Username is already taken" });
        }

        var emailExists = await _userManager.Users.AnyAsync(u => u.Email == registrationData.Email);
        if (emailExists)
        {
            return BadRequest(new { message = "Email is already registered" });
        }

        // Create the user
        var user = new User
                   {
                       UserName = request.Username, Email = registrationData.Email, EmailConfirmed = true, TosAcceptedAt = DateTime.UtcNow,
                       ReceivesNewsletter = request.ReceiveNewsletter
                   };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { message = "Failed to create user", errors = createResult.Errors.Select(e => e.Description).ToArray() });
        }

        await _userManager.AddToRoleAsync(user, nameof(UserRole.User));

        // Clear the cache
        _memoryCache.Remove($"google_registration_{request.TempToken}");

        var tokens = await _tokenService.GenerateTokens(user);
        await _context.SaveChangesAsync(); // Save refresh token
        return Ok(tokens);
    }

    private async Task<bool> ValidateRecaptcha(string recaptchaToken)
    {
        var recaptchaSecret = _configuration["Google:RecapatchaSecret"];
        if (string.IsNullOrWhiteSpace(recaptchaToken))
        {
            return false;
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        using (var http = new HttpClient())
        {
            var form = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("secret", recaptchaSecret),
                new KeyValuePair<string, string>("response", recaptchaToken),
                new KeyValuePair<string, string>("remoteip", remoteIp ?? string.Empty)
            ]);

            var verifyResponse = await http.PostAsync("https://www.google.com/recaptcha/api/siteverify", form);
            if (!verifyResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var verifyJson = await verifyResponse.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(verifyJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("success", out var successProperty) || !successProperty.GetBoolean() ||
                    root.TryGetProperty("score", out var scoreProperty) &&
                    scoreProperty.ValueKind == JsonValueKind.Number &&
                    scoreProperty.GetDouble() < 0.5)
                {
                    return false;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return true;
    }
}