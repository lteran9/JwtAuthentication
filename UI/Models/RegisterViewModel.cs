using System.ComponentModel.DataAnnotations;

namespace UI.Models;

public sealed class RegisterViewModel
{
    [Required, StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
