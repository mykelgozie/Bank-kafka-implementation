using Bank.Application.Interface;
using Bank.Domain.Dtos.Request;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private IProcessWebHookService _processWebHookService;
        private IConfiguration _configuration;

        public WebhookController(IProcessWebHookService processWebHookService, IConfiguration configuration)
        {
            _processWebHookService = processWebHookService;
            _configuration = configuration;
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> ProcessWebHook([FromBody] TransactionWebHookRequest transactionWebHookRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!IsValidApiKey())
            {
                return Unauthorized("Unauthorized access");
            }

            var response = await _processWebHookService.ProcessTransactionWebhook(transactionWebHookRequest);
            return response.Success ? Ok(response) : BadRequest(response);
        }


        [HttpPost("transactions-event")]
        public async Task<IActionResult> ProcessEventWebHook([FromBody] TransactionWebHookRequest transactionWebHookRequest)
        {
            var response = await _processWebHookService.ProcessTransactionWebhookV2(transactionWebHookRequest);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        private bool IsValidApiKey()
        {
            var requestKey = Request.Headers["x-api-key"].ToString();
            var validKey = _configuration["ApiKey"];
            return !string.IsNullOrEmpty(validKey) && string.Equals(requestKey, validKey, StringComparison.Ordinal);
        }
    }
}
