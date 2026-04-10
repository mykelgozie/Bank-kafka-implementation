using Bank.Application.Interface;
using Bank.Application.Service;
using Bank.Domain.Dtos.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TranstionController : ControllerBase
    {
        private IProcessWebHookService _processWebHookService;

        public TranstionController(IProcessWebHookService processWebHookService)
        {
            _processWebHookService = processWebHookService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ProcessWebHook([FromBody] TransactionWebHookRequest transactionWebHookRequest)
        {
            var response = await _processWebHookService.ProcessTransactionWebhook(transactionWebHookRequest);
            return response.Success ? Ok(response) : BadRequest(response);
        }


        [HttpPost("webhook-event")]
        public async Task<IActionResult> ProcessEventWebHook([FromBody] TransactionWebHookRequest transactionWebHookRequest)
        {
            var response = await _processWebHookService.ProcessTransactionWebhookV2(transactionWebHookRequest);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
