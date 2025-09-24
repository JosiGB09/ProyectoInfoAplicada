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
            // Validaciones básicas que se hacen antes de realizar la tarea en background
            if (request == null) return BadRequest("Request body is required.");
            if (request.CustomerId <= 0) return BadRequest("CustomerId inválido.");
            if (request.StartDate >= request.EndDate) return BadRequest("Rango de fechas inválido.");

            if (string.IsNullOrEmpty(request.CorrelationId))
                request.CorrelationId = Guid.NewGuid().ToString();

            var now = DateTime.UtcNow;
            var logMessage = $"Solicitud recibida: CorrelationId={request.CorrelationId}, CustomerId={request.CustomerId}, Rango={request.StartDate:O} - {request.EndDate:O}, Hora={now:O}";
            _logger.LogInformation(logMessage);
            await _kafka.SendLogAsync(logMessage);

            // Delay configurable (default 5 min)
            int delay = _configuration.GetValue<int?>("Hangfire:DelayMinutes") ?? 5;

            // Programar la tarea que invocará el ReportService.CallPdfApi(request)
            _backgroundJobs.Schedule<IReportService>(
                service => service.CallPdfApi(request),
                TimeSpan.FromMinutes(delay)
            );

            return Ok(new { Message = "Solicitud recibida y encolada.", CorrelationId = request.CorrelationId, DelayMinutes = delay });
        }

        // Endpoint de prueba simple ( esto es solo para pruebas, no es parte del flujo real)
        //se puede quitar luego
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] ReportRequest request)
        {
            var msg = $"Generate endpoint invoked: CorrelationId={request.CorrelationId}, CustomerId={request.CustomerId}";
            _logger.LogInformation(msg);
            await _kafka.SendLogAsync(msg);

            return Ok(new { Message = "Generate endpoint recibido (simulado)." });
        }
    }
}
