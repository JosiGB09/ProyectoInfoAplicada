using Microsoft.AspNetCore.Mvc;
using ServerHangfire.Models;
using ServerHangfire.Services;

namespace ServerHangfire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportHandlerService _reportHandler;

        public ReportsController(ReportHandlerService reportHandler)
        {
            _reportHandler = reportHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequest request)
        {
            if (!_reportHandler.ValidateRequest(request, out var error))
                return BadRequest(error);

            var correlationId = await _reportHandler.LogRequestAsync(request);
            _reportHandler.ScheduleReportJob(request);

            return Ok(new
            {
                Message = "Solicitud recibida y encolada.",
                CorrelationId = correlationId,
                DelayMinutes = "Configurado en appsettings.json"
            });
        }
    }
}