using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Features.Auth;

/// <summary>Credentials for creating a standard operator account.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, MinLength(8)]
    public required string Password { get; init; }
}

/// <summary>Credentials for logging in to the API.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

/// <summary>A bearer access token and its expiration time.</summary>
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt);
