using System.ComponentModel.DataAnnotations;

namespace API.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    // Supply the real value with user-secrets or the Jwt__Key environment variable. Never commit it to appsettings.json.
    [Required]
    public string Key { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; }
}
