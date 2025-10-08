using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ServerHangfire.Models;
using ServerHangfire.Services;

namespace ServerHangfire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly KafkaProducerService _kafka;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IConfiguration _configuration;

        public ReportsController(ILogger<ReportsController> logger,
                                 KafkaProducerService kafka,
                                 IBackgroundJobClient backgroundJobs,
                                 IConfiguration configuration)
        {
            _logger = logger;
            _kafka = kafka;
            _backgroundJobs = backgroundJobs;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequest request)
        {
            if (request == null) return BadRequest("Request body is required.");
            if (request.CustomerId <= 0) return BadRequest("CustomerId inválido.");
            if (request.StartDate >= request.EndDate) return BadRequest("Rango de fechas inválido.");

            if (string.IsNullOrEmpty(request.CorrelationId))
                request.CorrelationId = Guid.NewGuid().ToString();

            var now = DateTime.UtcNow;
            var logMsg = $"Solicitud recibida: CorrelationId={request.CorrelationId}, CustomerId={request.CustomerId}, Rango={request.StartDate:O} - {request.EndDate:O}, Hora={now:O}";
            _logger.LogInformation(logMsg);

            await _kafka.SendLogAsync(new LogEvent
            {
                CorrelationId = request.CorrelationId,
                Endpoint = "Reports/CreateReport",
                Message = "Solicitud recibida correctamente.",
                Success = true
            });

            int delay = _configuration.GetValue<int?>("Hangfire:DelayMinutes") ?? 5;

            _backgroundJobs.Schedule<IReportService>(
                service => service.CallPdfApi(request),
                TimeSpan.FromMinutes(delay)
            );

            return Ok(new
            {
                Message = "Solicitud recibida y encolada.",
                CorrelationId = request.CorrelationId,
                DelayMinutes = delay
            });
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] ReportRequest request)
        {
            _logger.LogInformation($"Generate endpoint invoked: CorrelationId={request.CorrelationId}, CustomerId={request.CustomerId}");
            await _kafka.SendLogAsync(new LogEvent
            {
                CorrelationId = request.CorrelationId,
                Endpoint = "Reports/generate",
                Message = "Generate endpoint invocado (simulado).",
                Success = true
            });

            return Ok(new { Message = "Generate endpoint recibido (simulado)." });
        }
    }
}
