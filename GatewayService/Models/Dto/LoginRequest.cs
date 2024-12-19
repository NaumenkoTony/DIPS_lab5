namespace GatewayService.Models.Dto;
public partial class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
