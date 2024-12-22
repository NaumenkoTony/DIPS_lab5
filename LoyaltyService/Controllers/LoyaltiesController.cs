namespace LoyaltyService.Controllers;

using AutoMapper;
using LoyaltyService.Data;
using LoyaltyService.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoyaltyService.ITokenService;

[Authorize]
public class LoyaltiesController(ILoyalityRepository repository, IMapper mapper, ITokenService tokenService) : Controller
{
    private readonly ILoyalityRepository repository = repository;
    private readonly IMapper mapper = mapper;
    private readonly ITokenService tokenService = tokenService;
    
    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyResponse>> GetByUsername()
    {
        string username = tokenService.GetUsernameFromJWT();
        return Ok(mapper.Map<LoyaltyResponse>(await repository.GetLoyalityByUsername(username)));
    }

    [Route("/api/v1/[controller]/improve")]
    [HttpGet]
    public async Task<ActionResult> ImproveLoyality()
    {
        string username = tokenService.GetUsernameFromJWT();
        await repository.ImproveLoyality(username);
        return Ok();
    }

    
    [Route("/api/v1/[controller]/degrade")]
    [HttpGet]
    public async Task<ActionResult> DegradeLoyality([FromHeader(Name = "X-User-Name")] string username)
    {
        await repository.DegradeLoyality(username);
        return Ok();
    }
}