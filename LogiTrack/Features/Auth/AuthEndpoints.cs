using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LogiTrack.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .AllowAnonymous()
            .WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register an operator account")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Log in and receive a bearer access token")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<IdentityUser> userManager,
        CancellationToken cancellationToken)
    {
        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
            return Results.ValidationProblem(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AuthRoles.Operator);
        if (!roleResult.Succeeded)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign the default account role or remove the incomplete account. Role errors: {string.Join(", ", roleResult.Errors.Select(error => error.Code))}. Cleanup errors: {string.Join(", ", deleteResult.Errors.Select(error => error.Code))}");
            }

            throw new InvalidOperationException(
                $"Could not assign the default account role: {string.Join(", ", roleResult.Errors.Select(error => error.Code))}");
        }

        return Results.Ok();
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Authentication attempt was rejected.");
            return Results.Unauthorized();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            logger.LogWarning("Authentication attempt was rejected.");
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        var userClaims = await userManager.GetClaimsAsync(user);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(60);
        var issuer = configuration["Authentication:Jwt:Issuer"] ?? "LogiTrack";
        var audience = configuration["Authentication:Jwt:Audience"] ?? "LogiTrack";
        var key = configuration["Authentication:Jwt:Key"]
            ?? throw new InvalidOperationException("The JWT signing key is not configured.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(userClaims.Where(claim => claim.Type != "role"));
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            expiresAt,
            signingCredentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new LoginResponse(accessToken, "Bearer", new DateTimeOffset(expiresAt)));
    }
}
