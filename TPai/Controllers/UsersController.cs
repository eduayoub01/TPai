using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPai.Models;
using TPai.Repositories;

namespace TPai.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditLogRepository _logRepository;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditLogRepository logRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logRepository = logRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "USER"
            });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim();
        var role = (dto.Role ?? "USER").Trim().ToUpperInvariant();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest($"Role \"{dto.Role}\" does not exist.");
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return Conflict("Email is already in use.");
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

        await _userManager.AddToRoleAsync(user, role);
        await _logRepository.LogAsync("CREATE", "User", $"Created user {user.Email} ({role})", User.Identity?.Name ?? "Unknown");

        return CreatedAtAction(nameof(GetAll), new UserDto
        {
            Id = user.Id,
            Email = email,
            Role = role
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound("User was not found.");
        }

        var email = (dto.Email ?? string.Empty).Trim();
        var role = (dto.Role ?? "USER").Trim().ToUpperInvariant();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest($"Role \"{dto.Role}\" does not exist.");
        }

        var emailOwner = await _userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            return Conflict("Email is already in use by another account.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var isCurrentlyAdmin = currentRoles.Contains("ADMIN");
        if (isCurrentlyAdmin && role != "ADMIN")
        {
            var admins = await _userManager.GetUsersInRoleAsync("ADMIN");
            if (admins.Count <= 1)
            {
                return BadRequest("At least one administrator account must remain.");
            }
        }

        user.Email = email;
        user.UserName = email;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, role);

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, token, dto.Password);
            if (!passResult.Succeeded)
            {
                return BadRequest(string.Join("; ", passResult.Errors.Select(e => e.Description)));
            }
        }

        await _logRepository.LogAsync("UPDATE", "User", $"Updated user {user.Email} ({role})", User.Identity?.Name ?? "Unknown");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound("User was not found.");
        }

        if (string.Equals(User.Identity?.Name, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("You cannot delete your own account while signed in.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("ADMIN"))
        {
            var admins = await _userManager.GetUsersInRoleAsync("ADMIN");
            if (admins.Count <= 1)
            {
                return BadRequest("At least one administrator account must remain.");
            }
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _logRepository.LogAsync("DELETE", "User", $"Deleted user {user.Email}", User.Identity?.Name ?? "Unknown");

        return NoContent();
    }
}
