using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFServer.Models;
using PDFServer.Services;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

namespace PDFServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PDFReportsController : ControllerBase
    {
        private readonly PDFService _pdfService;
        private readonly IHttpClientFactory _httpClientFactory;
        public PDFReportsController(PDFService pdfService, IHttpClientFactory httpClientFactory)
        {
            _pdfService = pdfService;
            _httpClientFactory = httpClientFactory;
        }
        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request)
        {
            Console.WriteLine($"Received request for CustomerId: {request.CustomerId}, CorrelationId: {request.CorrelationId}");
            try
            {
                await _pdfService.GenerateReportAsync(request.CustomerId, request.CorrelationId, request.StartDate, request.EndDate);
                await ScheduleNotificationTasks(request.CorrelationId, request);
                Console.WriteLine("Reporte generado exitosamente y tareas solicitdas");
                return Ok(new { Message = "Reporte generado exitosamente y tareas solicitdas" });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return StatusCode(500, new { Message = "Error al generar el reporte.", Details = exception.Message });
            }
        }
        private async Task ScheduleNotificationTasks(string correlationId, ReportRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            string hangfireUrl = "http://localhost:5294/api/reports";

            var payload = new
            {
                mensaje = "Success",
                correlationId = correlationId
            };
            var json=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8, "application/json");

            await client.PostAsync($"{hangfireUrl}/pdf-callback", json);
        }
        
    }
}
