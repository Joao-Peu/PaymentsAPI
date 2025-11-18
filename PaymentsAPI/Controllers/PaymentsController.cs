using Microsoft.AspNetCore.Mvc;
using PaymentsAPI.Core.Entities;
using PaymentsAPI.Infrastructure;
using PaymentsAPI.Core.ValueObjects;

namespace PaymentsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _repository;

    public PaymentsController(IPaymentRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId)
    {
        var payment = await _repository.GetByOrderIdAsync(orderId);
        if (payment == null) return NotFound();
        return Ok(payment);
    }
}
