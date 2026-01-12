using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Voltyks.Application.Interfaces.Paymob;
using Voltyks.Core.DTOs;
using Voltyks.Core.DTOs.Paymob.AddtionDTOs;
using Voltyks.Core.DTOs.Paymob.ApplePay;
using Voltyks.Core.DTOs.Paymob.CardsDTOs;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Core.DTOs.Paymob.intention;
using Voltyks.Core.DTOs.Paymob.Options;


namespace Voltyks.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymobService _svc;
        private readonly IHostEnvironment _env;
        private readonly PaymobOptions _opt;

        public PaymentController(
            IPaymobService svc,
            IHostEnvironment env,
            IOptions<PaymobOptions> opt)
        {
            _svc = svc;
            _env = env;
            _opt = opt.Value;
        }


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
        [RequestSizeLimit(1_048_576)] // 1 MB limit - prevents DoS attacks
        public async Task<IActionResult> Webhook()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            var res = await _svc.HandleWebhookAsync(Request, raw);
            return Ok(res); // مهم: 200 دائمًا لويبهوك بايموب
        }

        /// <summary>
        /// Test endpoint for webhook simulation (Development/Staging ONLY)
        /// Requires X-Webhook-Test-Key header with valid API key
        /// </summary>
        [HttpPost("webhook/test")]
        [AllowAnonymous]
        [RequestSizeLimit(1_048_576)]
        [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger in production
        public async Task<IActionResult> WebhookTest()
        {
            // Double protection: Environment check + API Key
            // 1) Environment check - only allow in Development or Staging
            var isAllowedEnv = _env.IsDevelopment() ||
                               _env.EnvironmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase);

            if (!isAllowedEnv)
            {
                return NotFound(); // Return 404 in Production (hide endpoint existence)
            }

            // 2) API Key check - require X-Webhook-Test-Key header
            var testKey = Request.Headers["X-Webhook-Test-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(testKey) ||
                string.IsNullOrWhiteSpace(_opt.WebhookTestKey) ||
                !testKey.Equals(_opt.WebhookTestKey, StringComparison.Ordinal))
            {
                return NotFound(); // Return 404 for invalid key (hide endpoint existence)
            }

            // Process webhook with HMAC verification skipped
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            var res = await _svc.HandleWebhookAsync(Request, raw, skipHmac: true);
            return Ok(res);
        }


        [HttpPost("intention")]
        public async Task<ActionResult<ApiResponse<CreateIntentResponse>>> CreateIntention([FromBody] CardCheckoutRequest req, CancellationToken ct)
        {
            if (req.AmountCents <= 0)
                return BadRequest("Invalid amount/currency");

            // Normalize payment method - now supports Card, Wallet, and ApplePay
            var method = req.PaymentMethod?.Trim()?.ToLowerInvariant() switch
            {
                "wallet" => "Wallet",
                "applepay" => "ApplePay",
                "apple_pay" => "ApplePay",
                _ => "Card"
            };

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


        // ===================== Apple Pay Server-to-Server =====================

        /// <summary>
        /// Process Apple Pay payment directly (Server-to-Server)
        /// Mobile app sends the Apple Pay token from PKPayment.token.paymentData
        /// </summary>
        /// <param name="req">Apple Pay request with token and billing data</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Payment result with transaction details</returns>
        [HttpPost("applepay/process")]
        [ProducesResponseType(typeof(ApiResponse<ApplePayProcessResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ApplePayProcessResponse>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ApplePayProcessResponse>>> ProcessApplePay(
            [FromBody] ApplePayDirectRequest req,
            CancellationToken ct = default)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<ApplePayProcessResponse>(
                    message: "Invalid request",
                    status: false,
                    errors: errors
                ));
            }

            var result = await _svc.ProcessApplePayAsync(req, ct);

            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Verify Apple Pay payment status by merchant order ID
        /// </summary>
        /// <param name="req">Request containing the merchant order ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Current payment status</returns>
        [HttpPost("applepay/verify")]
        [ProducesResponseType(typeof(ApiResponse<ApplePayVerifyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ApplePayVerifyResponse>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ApplePayVerifyResponse>>> VerifyApplePay(
            [FromBody] ApplePayVerifyRequest req,
            CancellationToken ct = default)
        {
            var result = await _svc.VerifyApplePayAsync(req.MerchantOrderId, ct);
            if (!result.Status)
                return NotFound(result);
            return Ok(result);
        }

    }
}
