namespace ipvcr.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class LoginRequest
{
    [Required]
    [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
    [JsonPropertyName("username")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    [JsonPropertyOrder(1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Password cannot be longer than 100 characters.")]
    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    [JsonPropertyOrder(2)]
    public string Password { get; set; } = string.Empty;
}
