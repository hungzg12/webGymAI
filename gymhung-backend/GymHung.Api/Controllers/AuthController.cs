using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
namespace GymManagement.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(AccountService accounts, WorkspaceService workspace, IConfiguration config) : ControllerBase
{
    [HttpPost("login"), EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var u = await accounts.LoginAsync(request, ct);
        var roles = await accounts.RolesAsync(u.UserId, ct);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, u.UserId.ToString()), new(JwtRegisteredClaimNames.Sub, u.UserId.ToString()), new(ClaimTypes.Name, u.FullName), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddMinutes(60), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        var session = JsonSerializer.SerializeToElement(await accounts.SessionAsync(u.UserId, ct));
        var result = session.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value);
        result["token"] = new JwtSecurityTokenHandler().WriteToken(jwt);
        result["expiresIn"] = 3600;
        return Ok(result);
    }
    [HttpGet("me"), Authorize] public async Task<IActionResult> Me(CancellationToken ct) => Ok(await accounts.SessionAsync(Id(), ct));
    [HttpPut("profile"), Authorize]
    public async Task<IActionResult> Profile(JsonElement request, CancellationToken ct)
    {
        var draft = request.TryGetProperty("draft", out var nested) ? nested : request;
        return Ok(await workspace.SaveAsync(User, "Users", Id().ToString(), new MutationRequest(draft), ct));
    }
    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request, CancellationToken ct)
    {
        await accounts.ChangePasswordAsync(Id(), request, ct);
        return NoContent();
    }
    [HttpPost("register-member"), EnableRateLimiting("login")] public async Task<IActionResult> Register(MemberRegistrationRequest request, CancellationToken ct) => Created("/api/auth/login", await accounts.RegisterAsync(request, ct));
    private long Id() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
