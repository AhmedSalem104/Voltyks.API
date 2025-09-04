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
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _svc;
        public PaymentController(IPaymobService svc) => _svc = svc;

    

        [HttpPost("checkout/card")]
        public async Task<ActionResult<ApiResponse<CardCheckoutResponse>>> CheckoutCard([FromBody] CardCheckoutRequest req)
        {
            var serviceDto = new CardCheckoutServiceDto
            {
                AmountCents = req.AmountCents,
                Billing = req.Billing,
                MerchantOrderId = Guid.NewGuid().ToString(),
                Currency = "EGP"
            };

            var res = await _svc.CheckoutCardAsync(serviceDto);
            return res.Status ? Ok(res) : BadRequest(res);
        }

        [HttpPost("checkout/wallet")]
        public async Task<ActionResult<ApiResponse<WalletCheckoutResponse>>> CheckoutWallet([FromBody] WalletCheckoutRequest req)
        {
            var serviceDto = new WalletCheckoutServiceDto
            {
                AmountCents = req.AmountCents,
                WalletPhone = req.WalletPhone,

                // إضافات
                MerchantOrderId = Guid.NewGuid().ToString(),
                Currency = "EGP"
            };

            var res = await _svc.CheckoutWalletAsync(serviceDto);
            return res.Status ? Ok(res) : BadRequest(res);
        }


        [HttpPost("getOrderStatus")]
        [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<ApiResponse<OrderStatusDto>>> GetPaymobOrderStatus([FromBody] PaymobOrderRequestDto request)
        {
            var res = await _svc.GetOrderStatusFromPaymobAsync(request.PaymobOrderId);

            if (!res.Status)
                return StatusCode(StatusCodes.Status502BadGateway, res);

            return Ok(res);
        }


        // ========= Webhook (لازم يفضل متاح) =========
        // ده لازم يبقى AllowAnonymous عشان Paymob يقدر يناديه
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            var res = await _svc.HandleWebhookAsync(Request, raw);
            return res.Status ? Ok(res) : BadRequest(res);
        }




    }
}
