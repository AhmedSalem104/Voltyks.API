using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
using Voltyks.Core.DTOs.Paymob.CardsDTOs;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Core.DTOs.Paymob.intention;


namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _svc;
        public PaymentController(IPaymobService svc) => _svc = svc;


        //[HttpPost("checkout/card")]
        //public async Task<ActionResult<ApiResponse<CardCheckoutResponse>>> CheckoutCard([FromBody] CardCheckoutRequest req)
        //{
        //    var serviceDto = new CardCheckoutServiceDto
        //    {
        //        AmountCents = req.AmountCents,
        //        MerchantOrderId = Guid.NewGuid().ToString(),
        //        Currency = "EGP",
        //        Billing = new BillingData
        //        (
        //            req.Billing.first_name,
        //            req.Billing.last_name,
        //            req.Billing.email,
        //            req.Billing.phone_number,
        //            "NA",  // apartment
        //            "NA",  // floor
        //            "NA",  // building
        //            "NA",  // street
        //            "NA",  // city
        //            "NA",  // state
        //            "EG",  // country
        //            "NA"   // postal_code

        //        )
        //    };

        //    var res = await _svc.CheckoutCardAsync(serviceDto);
        //    return res.Status ? Ok(res) : BadRequest(res);
        //}

        //[HttpPost("checkout/wallet")]
        //public async Task<ActionResult<ApiResponse<WalletCheckoutResponse>>> CheckoutWallet([FromBody] WalletCheckoutRequest req)
        //{
        //    var serviceDto = new WalletCheckoutServiceDto
        //    {
        //        AmountCents = req.AmountCents,
        //        WalletPhone = req.WalletPhone,

        //        // إضافات
        //        MerchantOrderId = Guid.NewGuid().ToString(),
        //        Currency = "EGP"
        //    };

        //    var res = await _svc.CheckoutWalletAsync(serviceDto);
        //    return res.Status ? Ok(res) : BadRequest(res);
        //}


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


        //[HttpPost("webhook")]
        //[AllowAnonymous]
        //public async Task<IActionResult> Webhook()
        //{
        //    using var reader = new StreamReader(Request.Body);
        //    var raw = await reader.ReadToEndAsync();
        //    var res = await _svc.HandleWebhookAsync(Request, raw);
        //    return res.Status ? Ok(res) : BadRequest(res);
        //}
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            var res = await _svc.HandleWebhookAsync(Request, raw);
            return Ok(res); // مهم: 200 دائمًا لويبهوك بايموب
        }







        //[HttpPost("intention")]
        //public async Task<ActionResult<ApiResponse<IntentionClientSecretDto>>> Intention(
        //    [FromBody] IntentionExchangeRequest req, CancellationToken ct)
        //{
        //    var res = await _svc.ExchangePaymentKeyForClientSecretAsync(req.PaymentKey, null, ct);
        //    return res.Status ? Ok(res) : BadRequest(res);
        //}

        [HttpPost("intention")]
        public async Task<ActionResult<ApiResponse<CreateIntentResponse>>> CreateIntention(
                                                    [FromBody] CardCheckoutRequest req, CancellationToken ct)
        {
            if (req.AmountCents <= 0)
                return BadRequest("Invalid amount/currency");

            // Normalize
            var method = req.PaymentMethod?.Trim();
            method = string.Equals(method, "Wallet", StringComparison.OrdinalIgnoreCase) ? "Wallet" : "Card";

            var dto = new CreateIntentRequest(
                amount: req.AmountCents,
                currency : "EGP",
                billingData: new BillingDataDto(
                    first_Name: req.Billing.first_name,
                    last_Name: req.Billing.last_name,
                    email: req.Billing.email,
                    phone_Number: req.Billing.phone_number,
                    country: "EG",
                    city: "NA",
                    state: "NA",
                    street: "NA",
                    building: "NA",
                    apartment: "NA",
                    floor: "NA",
                    postal_Code: "NA"
                ),
                merchantOrderId: Guid.NewGuid().ToString("N"),
                saveCard: req.SaveCard,
                paymentMethod: method
            );

            var result = await _svc.CreateIntentionAsync(dto, ct);
            if (!result.Status) return BadRequest(result);
            return Ok(result);
        }


        //[HttpPost("notification")]
        //public async Task<IActionResult> HandlePaymentNotification([FromBody] PaymentNotification notification, CancellationToken ct)
        //{
        //    var result = await _svc.HandlePaymentNotificationAsync(Request, notification.RawBody);

        //    if (result.Status)
        //    {
        //        return Ok(result);
        //    }


        //    return BadRequest(result);
        //}
        // ===================== Saved Cards / Tokenization =====================

        // بدء جلسة إضافة كارت (SDK هيعمل Save Card ويرجع Webhook بـ CARD_TOKEN)
        [HttpPost("tokenization")]
        public async Task<ActionResult<ApiResponse<TokenizationStartRes>>> StartCardTokenization()
        {
            var res = await _svc.StartCardTokenizationAsync();
            return res.Status ? Ok(res) : BadRequest(res);
        }

        // عرض كل الكروت المحفوظة للمستخدم الحالي
        [HttpGet("GetListOfCards")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SavedCardViewDto>>>> ListSavedCards()
        {
            var res = await _svc.ListSavedCardsAsync();
            return res.Status ? Ok(res) : BadRequest(res);
        }

        // تعيين كارت كافتراضي
        [HttpPost("setDefault_Card")]
        public async Task<ActionResult<ApiResponse<bool>>> SetDefaultCard([FromBody] SetDefaultCardRequestDto req)
        {
            var res = await _svc.SetDefaultCardAsync(req.CardId);
            return res.Status ? Ok(res) : BadRequest(res);
        }


        // حذف كارت محفوظ
        [HttpDelete("delete_Card")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCard([FromBody] DeleteCardRequestDto req)
        {
            var res = await _svc.DeleteCardAsync(req.CardId);
            return res.Status ? Ok(res) : BadRequest(res);
        }



        // ===================== Charge with Saved Card =====================

        // (1) الدفع بالسيرفر مباشرةً باستخدام الكارت المحفوظ
        [HttpPost("payWithSavedCard")]
        public async Task<ActionResult<ApiResponse<object>>> ChargeWithSavedCard([FromBody] ChargeWithSavedCardReq req)
        {
            var res = await _svc.ChargeWithSavedCardServerAsync(req);
            return res.Status ? Ok(res) : BadRequest(res);
        }




    }
}
