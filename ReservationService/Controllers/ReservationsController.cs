using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;
using ReservationService.Models.DomainModels;
using ReservationService.Models.Dto;
using ReservationService.TokenService;

namespace ReservationService.Controllers;

[Authorize]
public class ReservationsController(IReservationRepository repository, IHotelRepository hotelRepository, IMapper mapper, ILogger<ReservationsController> logger, ITokenService tokenService) : Controller
{
    private readonly IReservationRepository repository = repository;
    private readonly IHotelRepository hotelRepository = hotelRepository;
    private readonly IMapper mapper = mapper;
    private readonly ILogger<ReservationsController> logger = logger;
    private readonly ITokenService tokenService = tokenService;

    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationResponse>>> GetByUsernameAsync()
    {
        string username = tokenService.GetUsernameFromJWT();
        return Ok(mapper.Map<IEnumerable<ReservationResponse>>(await repository.GetReservationsByUsernameAsync(username)));
    }

    [Route("/api/v1/[controller]/")]
    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> CreateReservationAsync([FromBody] ReservationRequest reservationRequest)
    {
        Reservation reservation = mapper.Map<Reservation>(reservationRequest);
        await repository.CreateAsync(reservation);
        return Ok(reservation);
    }

    [Route("/api/v1/[controller]")]
    [HttpPut]
    public async Task<ActionResult<ReservationResponse>> UpdateReservationStatusAsync([FromBody] ReservationResponse reservationResponse)
    {
        var reservation = await repository.GetByUidAsync(reservationResponse.ReservationUid);
        var newModel = mapper.Map<Reservation>(reservationResponse);
        newModel.Id = reservation.Id;

        await repository.UpdateAsync(newModel, reservation.Id);
        return Ok(newModel);
    }

    [Route("/api/v1/[controller]/hotels/{id}")]
    [HttpGet]
    public async Task<ActionResult<HotelResponse>> GetHotelAsync(int id)
    {   
        return Ok(mapper.Map<HotelResponse>(await hotelRepository.ReadAsync(id)));
    }

    [Route("/api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<ReservationResponse>> GetReservationAsync(string uid)
    {   
        string username = tokenService.GetUsernameFromJWT();
        return Ok(mapper.Map<ReservationResponse>(await repository.GetByUsernameUidAsync(username, uid)));
    }
}