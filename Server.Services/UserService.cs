using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Data.Models;

namespace Server.Services;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly TokenSettings _tokenSettings;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context, 
        TokenSettings tokenSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _roleManager = roleManager;
         _tokenSettings = tokenSettings; 
    }
    
    public async Task<RegistrationResult> RegisterAsync(UserRegisterRequest req)
    {
        var registrationResult = new RegistrationResult(); 
        
        var user = new ApplicationUser
        {
            UserName = req.Username,
            Email = req.Email,
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        await _userManager.AddToRoleAsync(user, "User");

        if (!result.Succeeded)
        {
            registrationResult.Succeeded = false; 
            registrationResult.Errors.AddRange(result.Errors.Select(e => e.Description));
            return registrationResult;
        }

        registrationResult.Succeeded = true;
        return registrationResult;

    }

    public async Task<ApplicationUser?> LoginAsync(UserLoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
        {
            return null; 
        }
        else
        {
            var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, true);
            return user;
        }
    }

    public async Task<string> GenerateToken(ApplicationUser user)
    {
        var claims = (from ur in _context.UserRoles
                where ur.UserId == user.Id
                join r in _context.Roles on ur.RoleId equals r.Id
                join rc in _context.RoleClaims on r.Id equals rc.RoleId
                select rc)
            .Where(rc => !string.IsNullOrEmpty(rc.ClaimValue) && !string.IsNullOrEmpty(rc.ClaimType))
            .Select(rc => new Claim(rc.ClaimType!, rc.ClaimValue!))
            .Distinct()
            .ToList();

        var roleClaims = (from ur in _context.UserRoles
                where ur.UserId == user.Id
                join r in _context.Roles on ur.RoleId equals r.Id
                select r)
            .Where(r => !string.IsNullOrEmpty(r.Name))
            .Select(r => new Claim(ClaimTypes.Role, r.Name!))
            .Distinct()
            .ToList();

        claims.AddRange(roleClaims);

        var token = GetToken(user, claims);
        return token; 
    }

    public async Task<RefreshTokenResult > RefreshToken(string refreshToken, ClaimsPrincipal userPrincipal)
    {
        
        
        var user = await _userManager.GetUserAsync(userPrincipal);
        if (user == null)
        {
            return (new RefreshTokenResult{Succeeded = false, Token = null, RefreshToken = null, Error = "User is not authenticated"});
        }

        var isValidToken =
            await _userManager.VerifyUserTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken", refreshToken);
        if (!isValidToken)
        {
            return (new RefreshTokenResult{Succeeded = false, Token = null, RefreshToken = null, Error = "Invalid or expired refresh token"});

        }

        await _userManager.RemoveAuthenticationTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken");
        var newToken = await GenerateToken(user);
        var newRefreshToken = await GenerateRefreshToken(user);

        return (new RefreshTokenResult{Succeeded = true, Token = newToken, RefreshToken = newRefreshToken, Error = null});

    }
    
    public async Task<string> GenerateRefreshToken(ApplicationUser user)
    {
        var refreshToken = await _userManager.GenerateUserTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken");
        await _userManager.SetAuthenticationTokenAsync(user, "REFRESHTOKENPROVIDER", "RefreshToken", refreshToken);
        return refreshToken;
    }
    
    private string GetToken(ApplicationUser user, List<Claim> claims)
    {
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256);
        var userClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("Username", user.UserName),
            
        };
        
        userClaims.AddRange(claims);
        var token = new JwtSecurityToken(
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            claims: userClaims,
            expires: DateTime.Now.AddSeconds(_tokenSettings.TokenExpireSeconds),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
}