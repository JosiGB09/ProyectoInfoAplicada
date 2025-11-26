using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFServer.Models;
using PDFServer.Services;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PDFServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfReportsController : ControllerBase
    {
        private readonly PdfService _pdfService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _hangfireUrl;

        public PdfReportsController(PdfService pdfService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _pdfService = pdfService;
            _httpClientFactory = httpClientFactory;
            _hangfireUrl = configuration.GetValue<string>("URLs:HangfireUrl") ?? throw new ArgumentNullException(nameof(configuration), "La URL de Hangfire no puede ser nula.");
        }
        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request)
        {
            Console.WriteLine($"Received request for CustomerId: {request.CustomerId}, CorrelationId: {request.CorrelationId}");
            try
            {
                await _pdfService.GenerateReportAsync(request.CustomerId, request.CorrelationId, request.StartDate, request.EndDate);
                await ScheduleNotificationTasks(request.CorrelationId);
                Console.WriteLine("Reporte generado exitosamente y tareas solicitdas");
                return Ok(new { Message = "Reporte generado exitosamente y tareas solicitdas" });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return StatusCode(500, new { Message = "Error al generar el reporte.", Details = exception.Message });
            }
        }
        private async Task ScheduleNotificationTasks(string correlationId)
        {
            var client = _httpClientFactory.CreateClient();

            var payload = new
            {
                mensaje = "Success",
                correlationId = correlationId
            };
            var json=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8, "application/json");

            await client.PostAsync($"{_hangfireUrl}/pdf-callback", json);
        }
        
    }
}
