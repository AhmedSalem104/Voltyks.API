using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Application.Services.Paymob;
using Voltyks.Application.ServicesManager.ServicesManager;
using Voltyks.Core.DTOs.Charger;
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
        //private readonly PaymobOptions _opt; 

        public PaymentController(IPaymobService svc, IOptions<PaymobOptions> opt)
        {
            _svc = svc;
           // _opt = opt.Value;
        }

        // 0) Auth Token
        [HttpPost("auth-token")]
        public async Task<ActionResult<string>> GetAuthToken()
        {
            var token = await _svc.GetAuthTokenAsync();
            return Ok(token);
        }

        // 1) Create Order
        [HttpPost("orders")]
        public async Task<ActionResult<int>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var orderId = await _svc.CreateOrderAsync(dto);
            return Ok(orderId);
        }

        // 2) Create Payment Key
        [HttpPost("payment-keys")]
        public async Task<ActionResult<string>> CreatePaymentKey([FromBody] CreatePaymentKeyDto dto)
        {
            var key = await _svc.CreatePaymentKeyAsync(dto);
            return Ok(key);
        }

        // 3) Build Card iFrame URL 
        [HttpPost("iframe-url")]
        public ActionResult<string> BuildIframeUrl([FromBody] BuildIframeUrlRequest dto)
        {
            var url = _svc.BuildCardIframeUrl(dto.PaymentKey);
            return Ok(url);
        }

        // 4) Wallet Pay
        [HttpPost("wallet-pay")]
        public async Task<ActionResult<PayActionRes>> WalletPay([FromBody] WalletPaymentDto dto)
        {
            var res = await _svc.PayWithWalletAsync(dto);
            return Ok(res);
        }

        // 5) Verify HMAC
        [HttpPost("verify-hmac")]
        public ActionResult<bool> VerifyHmac([FromBody] HmacVerifyDto dto)
        {
            var ok = _svc.VerifyHmac(dto);
            return Ok(ok);
        }

        // 6) Inquiry
        [HttpPost("inquiry")]
        public async Task<ActionResult<InquiryRes>> Inquiry([FromBody] InquiryDto dto)
        {
            var res = await _svc.InquiryAsync(dto);
            return Ok(res);
        }

        // 7) Refund
        [HttpPost("refund")]
        public async Task<ActionResult<PayActionRes>> Refund([FromBody] RefundDto dto)
        {
            var res = await _svc.RefundAsync(dto);
            return Ok(res);
        }

        // 8) Void
        [HttpPost("void")]
        public async Task<ActionResult<PayActionRes>> VoidPayment([FromBody] VoidDto dto)
        {
            var res = await _svc.VoidAsync(dto);
            return Ok(res);
        }

        // 9) Capture
        [HttpPost("capture")]
        public async Task<ActionResult<PayActionRes>> Capture([FromBody] CaptureDto dto)
        {
            var res = await _svc.CaptureAsync(dto);
            return Ok(res);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            // read raw body once
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                rawBody = await reader.ReadToEndAsync();

            var resp = await _svc.HandleWebhookAsync(Request, rawBody);

            // نرجّع 200 دايمًا عشان Paymob مايعيدش الإرسال
            // لو حابب تشوف الاستجابة الموحّدة، بص على resp (ApiResponse<bool>)
            return Ok(resp.Status ? "OK" : "INVALID_HMAC");
        }

    }


}
