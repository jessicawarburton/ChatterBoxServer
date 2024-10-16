using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Register(UserRegisterRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var result = await _userService.RegisterAsync(req);
        
        if (result.Succeeded)
        {
            return Ok(new { Message = "Registration successful" });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Login(UserLoginRequest req)
    {
        var user = await _userService.LoginAsync(req);
        if (user == null) return Unauthorized(new { Message = "Invalid username or password" });
        var accessToken = await _userService.GenerateToken(user);
        var refreshToken = await _userService.GenerateRefreshToken(user);
        return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        
        var result = await _userService.RefreshToken(request.RefreshToken, User);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = result.Error });
        }

        return Ok(new
        {
            AccessToken = result.Token,
            RefreshToken = result.RefreshToken
        });

    }
    
    
}