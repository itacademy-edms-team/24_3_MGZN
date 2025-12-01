using Microsoft.AspNetCore.Mvc;
using InShopBLLayer.Services;
using Contracts.Dtos;
using InShopBLLayer.Abstractions;

namespace InShop.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly IEmailVerificationService _verificationService;

        public VerificationController(IEmailVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] SendCodeRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email обязателен.");

            try
            {
                await _verificationService.GenerateAndSendCodeAsync(request.Email);
                return Ok(new { message = "Код отправлен на email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка отправки кода.", details = ex.Message });
            }
        }
        [HttpPost("validate-code")]
        public IActionResult ValidateCode([FromBody] ValidateCodeRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Code))
                return BadRequest("Email и код обязательны.");

            var isValid = _verificationService.ValidateCode(request.Email, request.Code);

            if (isValid)
                return Ok(new { success = true, message = "Код верен." });
            else
                return BadRequest(new { success = false, message = "Неверный или просроченный код." });
        }
    }
}
