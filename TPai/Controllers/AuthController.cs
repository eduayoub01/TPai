using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TPai.Models;
using TPai.Repositories;

namespace TPai.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogRepository _logRepository;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IAuditLogRepository logRepository)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logRepository = logRepository;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        await _logRepository.LogAsync("LOGIN", "Auth", "User logged in", user.Email ?? dto.Email);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email ?? dto.Email,
            Roles = roles
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return BadRequest("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, "USER");
        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        await _logRepository.LogAsync("CREATE", "Auth", "User registered", user.Email ?? dto.Email);

        return Ok(new AuthResponseDto
        {
            Token = token,
            Email = user.Email ?? dto.Email,
            Roles = roles
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var email = User.Identity?.Name ?? "Unknown";
        await _logRepository.LogAsync("LOGOUT", "Auth", "User logged out", email);
        return Ok();
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var duration = int.TryParse(jwtSection["DurationInMinutes"], out var mins) ? mins : 120;

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(duration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
