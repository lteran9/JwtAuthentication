using System.ComponentModel.DataAnnotations;

namespace API.Auth;

// Put validation attributes on the primary-constructor parameters. [property: ...] puts them on the property, and MVC throws on records that have validation on properties.
public sealed record RegisterRequest(
    [Required, StringLength(256)] string UserName,
    [Required, StringLength(128)] string Password);

public sealed record LoginRequest(
    [Required, StringLength(256)] string UserName,
    [Required, StringLength(128)] string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
