namespace GatewayService.Models.Dto;

public class UserInfoResponse
{
    public List<ReservationResponse> Reservations { get; set; } = [];
    public required LoyaltyInfoResponse Loyalty { get; set; }
}