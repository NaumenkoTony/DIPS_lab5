namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using GatewayService.Models.Dto;
using Newtonsoft.Json;
using AutoMapper;
using System.Text;
using GatewayService.Models.ReservationServiceDto;
using GatewayService.Models.LoyaltyServiceDto;
using GatewayService.Models.PaymentServiceDto;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;

[Authorize]
public class GatewayController(IHttpClientFactory httpClientFactory, IMapper mapper, ILogger<GatewayController> logger, 
ITokenService tokenService) : ControllerBase
{
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IMapper mapper = mapper;
    private readonly ILogger<GatewayController> logger = logger;
    private readonly ITokenService tokenService = tokenService;

    private async Task<ReservationResponse> CreateReservationResponseAsync(ReservationServiceResponse reservationServiceResponse)
    {
        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var paymentService = httpClientFactory.CreateClient("PaymentService");

        var hotelResponse = await reservationService.GetAsync($"/api/v1/reservations/hotels/{reservationServiceResponse.HotelId}");
        var hotelJsonString = await hotelResponse.Content.ReadAsStringAsync();
        var hotel = JsonConvert.DeserializeObject<HotelServiceResponse>(hotelJsonString);
        var hotelInfo = mapper.Map<HotelInfo>(hotel);

        var paymentResponse = await paymentService.GetAsync($"/api/v1/payments/{reservationServiceResponse.PaymentUid}");
        var paymentJsonString = await paymentResponse.Content.ReadAsStringAsync();
        var payment = JsonConvert.DeserializeObject<PaymentServiceResponse>(paymentJsonString);
        var paymentInfo = mapper.Map<PaymentInfo>(payment);

        var reservationResponse = new ReservationResponse
        {
            ReservationUid = reservationServiceResponse.ReservationUid,
            Hotel = hotelInfo,
            StartDate =  reservationServiceResponse.StartDate,
            EndDate = reservationServiceResponse.EndDate,
            Status = reservationServiceResponse.Status,
            Payment = paymentInfo
        };

        return reservationResponse;
    }

    [Route("api/v1/hotels")]
    [HttpGet]
    public async Task<ActionResult<PaginationResponse>> GetHotelsAsync([FromQuery] int page, [FromQuery] int size)
    {
        if (page < 0 || size < 1)
        {
            return BadRequest(new { message = "Invalid pagination parameters" });
        }

        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var hotelsResponse = await reservationService.GetAsync($"/api/v1/hotels?page={page}&size={size}");
        if (!hotelsResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)hotelsResponse.StatusCode, new { message = "Error receiving hotels from microservice" });
        }
        var hotelsJsonString = await hotelsResponse.Content.ReadAsStringAsync();
        var hotelsServiceResponse = JsonConvert.DeserializeObject<List<HotelServiceResponse>>(hotelsJsonString);
        if (hotelsServiceResponse == null)
        {
            return StatusCode(500, new { message = "Error parsing hotel data" });
        }

        var pagedResponse = new PaginationResponse
        {
            Page = page,
            PageSize = size,
            TotalElements = hotelsServiceResponse.Count,
            Items = mapper.Map<List<HotelResponse>>(hotelsServiceResponse)
        };

        return Ok(pagedResponse);
    }


    [Route("/api/v1/me")]
    [HttpGet]
    public async Task<ActionResult<UserInfoResponse>> GetInfoAsync()
    {
        var reservationService = httpClientFactory.CreateClient("ReservationService");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations");
        var reservationsResponse = await reservationService.SendAsync(request);
        if (!reservationsResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationsResponse.StatusCode, new { message = "Error receiving reservations from microservice" });
        }
        var reservationsJsonString = await reservationsResponse.Content.ReadAsStringAsync();
        var reservations = JsonConvert.DeserializeObject<List<ReservationServiceResponse>>(reservationsJsonString);
        if (reservations == null)
        {
            return StatusCode(500, new { message = "Error parsing reservation data" });
        }

        var paymentService = httpClientFactory.CreateClient("PaymentService");
        List<ReservationResponse> reservationResponses = [];
        foreach (var reservation in reservations)
        {
            reservationResponses.Add(await CreateReservationResponseAsync(reservation));
        }
        
        var loyaltyService = httpClientFactory.CreateClient("LoyaltyService");
        request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties");
        var loyaltiesResponse = await loyaltyService.SendAsync(request);
        if (!loyaltiesResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)loyaltiesResponse.StatusCode, new { message = "Error receiving loyalty from microservice" });
        }

        var loyaltiesResponseJsonString = await loyaltiesResponse.Content.ReadAsStringAsync();
        var loyalty = JsonConvert.DeserializeObject<LoyaltyServiceResponse>(loyaltiesResponseJsonString);
        if (loyalty == null)
        {
            return StatusCode(500, new { message = "Error parsing loyalty data" });
        }

        UserInfoResponse userInfoResponse = new UserInfoResponse
        {
            Reservations = reservationResponses,
            Loyalty = mapper.Map<LoyaltyInfoResponse>(loyalty) 
        };

        return Ok(userInfoResponse);
    }

    [Route("api/v1/loyalty")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyInfoResponse>> GetLoyalty()
    {
        var loyaltyService = httpClientFactory.CreateClient("LoyaltyService");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties");
        var loyaltyResponse = await loyaltyService.SendAsync(request);
        if (!loyaltyResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)loyaltyResponse.StatusCode, new { message = "Error receiving loyalties from microservice" });
        }
        var loyaltyJsonString = await loyaltyResponse.Content.ReadAsStringAsync();
        var loyaltyServiceResponse = JsonConvert.DeserializeObject<LoyaltyServiceResponse>(loyaltyJsonString);
        if (loyaltyServiceResponse == null)
        {
            return StatusCode(500, new { message = "Error parsing loyalty data" });
        }

        return Ok(mapper.Map<LoyaltyInfoResponse>(loyaltyServiceResponse));
    }

    [Route("api/v1/reservations")]
    [HttpPost]
    public async Task<ActionResult<LoyaltyInfoResponse>> BookHotel([FromBody] CreateReservationRequest createReservationRequest)
    {
         if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var hotelResponse = await reservationService.GetAsync($"/api/v1/hotels/{createReservationRequest.HotelUid}");
        if (!hotelResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)hotelResponse.StatusCode, new { message = $"Error receiving hotel from microservice" });
        }
        var hotelJsonString = await hotelResponse.Content.ReadAsStringAsync();
        var hotel = JsonConvert.DeserializeObject<HotelServiceResponse>(hotelJsonString);
        if (null == hotel)
        {
            return NotFound($"Hotel with uid {createReservationRequest.HotelUid} was not found");
        }

        DateTime startDate = DateTime.Parse(createReservationRequest.StartDate);
        DateTime endDate = DateTime.Parse(createReservationRequest.EndDate);
        int nightsNumber = (endDate - startDate).Days;
        int cost = nightsNumber * hotel.Price;
        var loyaltyService = httpClientFactory.CreateClient("LoyaltyService");
        
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties");
        var loyaltyResponse = await loyaltyService.SendAsync(request);
        if (!loyaltyResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)loyaltyResponse.StatusCode, new { message = "Error receiving loyalties from microservice" });
        }
        var loyaltyJsonString = await loyaltyResponse.Content.ReadAsStringAsync();
        var loyaltyServiceResponse = JsonConvert.DeserializeObject<LoyaltyServiceResponse>(loyaltyJsonString);
        if (loyaltyServiceResponse == null)
        {
            return StatusCode(500, new { message = "Error parsing loyalty data" });
        }
        var loyalty = mapper.Map<LoyaltyInfoResponse>(loyaltyServiceResponse);

        double discount = loyalty.Discount;
        int costWithDiscount = Convert.ToInt32(cost * (100 - discount) / 100);

        var paymentRequest = new PaymentServiceRequest
        {
            Status = PaymentOperationStatus.PAID.ToString(),
            Price = costWithDiscount
        };
        var paymentClient = httpClientFactory.CreateClient("PaymentService");
        string paymentJsonContent = System.Text.Json.JsonSerializer.Serialize(paymentRequest);
        var paymentContent = new StringContent(paymentJsonContent, Encoding.UTF8, "application/json");
        var paymentResponse = await paymentClient.PostAsync("/api/v1/payments", paymentContent);
        if (!paymentResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)paymentResponse.StatusCode, new { message = "Error posting payments from microservice" });
        }
        var paymentResponseJsonString = await paymentResponse.Content.ReadAsStringAsync();
        var payment = JsonConvert.DeserializeObject<PaymentServiceResponse>(paymentResponseJsonString);
        if (null == payment)
        {
            return NotFound($"Payment was not created");
        }
        
        string username = tokenService.GetUsernameFromJWT();
        var reservationRequest = new ReservationServiceRequest
        {
            Username = username,
            HotelId = hotel.Id,
            StartDate = startDate.ToString(),
            EndData = endDate.ToString(),
            Status = ReservationStatus.PAID.ToString(),
            PaymentUid = Convert.ToString(payment.PaymentUid)
        };
        var reservationJsonContent = System.Text.Json.JsonSerializer.Serialize(reservationRequest);
        var reservationContent = new StringContent(reservationJsonContent, Encoding.UTF8, "application/json");
        var reservationСlient = httpClientFactory.CreateClient("ReservationService");
        var reservationResponseBody = await reservationСlient.PostAsync("/api/v1/reservations/", reservationContent);
        if (!reservationResponseBody.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationResponseBody.StatusCode, new { message = "Error creating reservation from microservice" });
        }
        var reservationResponseBodyJsonString = await reservationResponseBody.Content.ReadAsStringAsync();
        var reservation = JsonConvert.DeserializeObject<ReservationServiceResponse>(reservationResponseBodyJsonString);
        if (null == reservation)
        {
            return NotFound($"Reservation was not created");
        }

        var loyaltyClient = httpClientFactory.CreateClient("LoyaltyService");
        request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/improve");
        loyaltyResponse = await loyaltyService.SendAsync(request);

        var createReservationResponse = new CreateReservationResponse
        {
            ReservationUid = reservation.ReservationUid,
            HotelUid = hotel.HotelUid,
            StartDate = createReservationRequest.StartDate,
            EndDate = createReservationRequest.EndDate,
            Discount = discount,
            Status = paymentRequest.Status,
            Payment = mapper.Map<PaymentInfo>(payment)
        };

        return Ok(createReservationResponse);    
    }

    
    [Route("api/v1/reservations/{reservationUid}")]
    [HttpGet]
    public async Task<ActionResult<ReservationResponse>> GetReservationByUsername(string reservationUid)
    {
        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations/{reservationUid}");
        var reservationServiceResponse = await reservationService.SendAsync(request);
        if (!reservationServiceResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationServiceResponse.StatusCode, new { message = $"Error receiving hotel from microservice" });
        }
        var reservationJsonString = await reservationServiceResponse.Content.ReadAsStringAsync();
        var reservation = JsonConvert.DeserializeObject<ReservationServiceResponse>(reservationJsonString);
        if (null == reservation)
        {
            return NotFound($"Reservations with uid {reservationUid} was not found");
        }

        ReservationResponse reservationResponse = await CreateReservationResponseAsync(reservation);

        return Ok(reservationResponse);
    }

    [Route("api/v1/reservations")]
    [HttpGet]
    public async Task<ActionResult<ReservationResponse>> GetReservationsByUsername()
    {
        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations");
        var reservationsResponse = await reservationService.SendAsync(request);
        if (!reservationsResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationsResponse.StatusCode, new { message = "Error receiving reservations from microservice" });
        }
        var reservationsJsonString = await reservationsResponse.Content.ReadAsStringAsync();
        var reservations = JsonConvert.DeserializeObject<List<ReservationServiceResponse>>(reservationsJsonString);
        if (reservations == null)
        {
            return StatusCode(500, new { message = "Error parsing reservation data" });
        }

        var paymentService = httpClientFactory.CreateClient("PaymentService");
        List<ReservationResponse> reservationResponses = [];
        foreach (var reservation in reservations)
        {
            reservationResponses.Add(await CreateReservationResponseAsync(reservation));
        }

        return Ok(reservationResponses);
    }

    [Route("api/v1/reservations/{reservationUid}")]
    [HttpDelete]
    public async Task<ActionResult<ReservationResponse>> UnbookHotel(string reservationUid)
    {
        var reservationService = httpClientFactory.CreateClient("ReservationService");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations/{reservationUid}");
        var reservationServiceResponse = await reservationService.SendAsync(request);
        if (!reservationServiceResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationServiceResponse.StatusCode, new { message = $"Error receiving reservation from microservice" });
        }
        var reservationJsonString = await reservationServiceResponse.Content.ReadAsStringAsync();
        var reservation = JsonConvert.DeserializeObject<ReservationServiceResponse>(reservationJsonString);
        if (null == reservation)
        {
            return NotFound($"Reservations with uid {reservationUid} was not found");
        }

        reservation.Status = ReservationStatus.CANCELED.ToString();
        request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/reservations");
        var json = JsonConvert.SerializeObject(reservation);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        reservationServiceResponse = await reservationService.SendAsync(request);
        if (!reservationServiceResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)reservationServiceResponse.StatusCode, new { message = $"Error updating reservation" });
        }
        
        var paymentService = httpClientFactory.CreateClient("PaymentService");
        var paymentServiceResponse = await paymentService.GetAsync($"/api/v1/payments/{reservation.PaymentUid}");
        var paymentJsonString = await paymentServiceResponse.Content.ReadAsStringAsync();
        var payment = JsonConvert.DeserializeObject<PaymentServiceResponse>(paymentJsonString);
        if (null == payment)
        {
            return NotFound($"Payment with uid {reservation.PaymentUid} was not found");
        }
        payment.Status = PaymentOperationStatus.CANCELED.ToString();
        request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/payments");
        json = JsonConvert.SerializeObject(payment);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        paymentServiceResponse = await paymentService.SendAsync(request);
        if (!paymentServiceResponse.IsSuccessStatusCode)
        {
            return StatusCode((int)paymentServiceResponse.StatusCode, new { message = $"Error updating payment" });
        }

        var loyaltyClient = httpClientFactory.CreateClient("LoyaltyService");
        request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/degrade");
        var loyaltyResponse = await loyaltyClient.SendAsync(request);

        return NoContent();
    }
}
