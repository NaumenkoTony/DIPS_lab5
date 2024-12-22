namespace PaymentService.Controllers;

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Data;
using PaymentService.Models.DomainModels;
using PaymentService.Models.Dto;
using PaymentService.ITokenService;

[Authorize]
public class PaymentsController(IPaymentRepository repository, IMapper mapper, ITokenService tokenService) : Controller
{
    private readonly IPaymentRepository repository = repository;
    private readonly IMapper mapper = mapper;
    private readonly ITokenService tokenService = tokenService;

    [Route("api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<PaymentResponse>> GetAsync(string uid)
    {   
        return Ok(mapper.Map<PaymentResponse>(await repository.GetByUidAsync(uid)));
    }

    [Route("api/v1/[controller]")]
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] PaymentRequest paymentRequest)
    {
        var payment = mapper.Map<Payment>(paymentRequest);
        
        await repository.CreateAsync(payment);

        var paymentResponse = mapper.Map<PaymentResponse>(payment);

        return Ok(paymentResponse);
    }

    [Route("/api/v1/[controller]")]
    [HttpPut]
    public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatusAsync([FromBody] PaymentResponse paymentResponse)
    {
        var payment = await repository.GetByUidAsync(paymentResponse.PaymentUid);
        var newModel = mapper.Map<Payment>(paymentResponse);
        newModel.Id = payment.Id;

        await repository.UpdateAsync(newModel, payment.Id);
        return Ok(newModel);
    }
}