using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Application.Services.Paymob;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Charger;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Core.DTOs.Paymob.Generic_Result_DTOs;
using Voltyks.Core.DTOs.Paymob.Input_DTOs;
using Voltyks.Core.DTOs.Paymob.Options;
using Voltyks.Persistence.Entities;

namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/Payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _svc;
        public PaymentController(IPaymobService svc) => _svc = svc;

        [HttpPost("auth-token")]
        public async Task<ActionResult<ApiResponse<string>>> GetAuthToken() => Ok(await _svc.GetAuthTokenAsync());

        [HttpPost("service-orders")]
        public async Task<ActionResult<ApiResponse<string>>> CreateServiceOrder([FromBody] CreateServiceOrderDto dto)
        {
            var res = await _svc.CreateServiceOrderAsync(dto);
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpGet("payment-methods")]
        public ActionResult<ApiResponse<PaymentMethodsDto>> GetPaymentMethods([FromQuery] string merchantOrderId)
            => Ok(_svc.GetAvailableMethods(merchantOrderId));

        [HttpPost("checkout/card")]
        public async Task<ActionResult<ApiResponse<CardCheckoutResponse>>> CheckoutCard([FromBody] CardCheckoutRequest req)
        {
            var res = await _svc.CheckoutCardAsync(req);
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpPost("checkout/wallet")]
        public async Task<ActionResult<ApiResponse<WalletCheckoutResponse>>> CheckoutWallet([FromBody] WalletCheckoutRequest req)
        {
            var res = await _svc.CheckoutWalletAsync(req);
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpPost("checkout/wallet-only")]
        public async Task<ActionResult<ApiResponse<WalletOnlyResponse>>> CheckoutWalletOnly([FromBody] WalletOnlyRequest req)
        {
            var res = await _svc.CheckoutWalletOnlyAsync(req);
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpGet("status")]
        public async Task<ActionResult<ApiResponse<OrderStatusDto>>> GetStatus([FromQuery] string merchantOrderId)
            => Ok(await _svc.GetStatusAsync(merchantOrderId));

        [HttpPost("orders")]
        public async Task<ActionResult<ApiResponse<int>>> CreateOrder([FromBody] CreateOrderDto dto)
            => Ok(await _svc.CreateOrderAsync(dto));

        [HttpPost("payment-keys")]
        public async Task<ActionResult<ApiResponse<string>>> CreatePaymentKey([FromBody] CreatePaymentKeyDto dto)
            => Ok(await _svc.CreatePaymentKeyAsync(dto));

        [HttpPost("iframe-url")]
        public ActionResult<ApiResponse<string>> BuildIframeUrl([FromBody] BuildIframeUrlRequest dto)
            => Ok(_svc.BuildCardIframeUrl(dto.PaymentKey));

        [HttpPost("wallet-pay")]
        public async Task<ActionResult<ApiResponse<PayActionRes>>> WalletPay([FromBody] WalletPaymentDto dto)
            => Ok(await _svc.PayWithWalletAsync(dto));

        [HttpPost("verify-hmac")]
        public ActionResult<ApiResponse<bool>> VerifyHmac([FromBody] HmacVerifyDto dto)
            => Ok(_svc.VerifyHmac(dto));

        [HttpPost("inquiry")]
        public async Task<ActionResult<ApiResponse<InquiryRes>>> Inquiry([FromBody] InquiryDto dto)
            => Ok(await _svc.InquiryAsync(dto));

        [HttpPost("refund")]
        public async Task<ActionResult<ApiResponse<PayActionRes>>> Refund([FromBody] RefundDto dto)
            => Ok(await _svc.RefundAsync(dto));

        [HttpPost("void")]
        public async Task<ActionResult<ApiResponse<PayActionRes>>> VoidPayment([FromBody] VoidDto dto)
            => Ok(await _svc.VoidAsync(dto));

        [HttpPost("capture")]
        public async Task<ActionResult<ApiResponse<PayActionRes>>> Capture([FromBody] CaptureDto dto)
            => Ok(await _svc.CaptureAsync(dto));

        [HttpGet("return")]
        [AllowAnonymous]
        public IActionResult Return([FromQuery] string merchantOrderId)
            => Ok(new { message = "Verifying payment, please wait...", merchantOrderId });

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                rawBody = await reader.ReadToEndAsync();

            var resp = await _svc.HandleWebhookAsync(Request, rawBody);
            return Ok(resp.Status ? "OK" : "INVALID_HMAC");
        }
    }



}
