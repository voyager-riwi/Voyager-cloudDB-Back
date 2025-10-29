using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetUserProfile()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        var profileData = new
        {
            Message = "Este es un endpoint protegido, funciona hpp",
            UserId = userId,
            Email = userEmail
        };
        return Ok(profileData);
    }
}