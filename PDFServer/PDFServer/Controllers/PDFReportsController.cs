using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PDFServer.Models;
using PDFServer.Services;
using System.Threading.Tasks;

namespace PDFServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PDFReportsController : ControllerBase
    {
        private readonly PDFService _pdfService;
        public PDFReportsController(PDFService pdfService)
        {
            _pdfService = pdfService;
        }
        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request)
        {
            Console.WriteLine($"Received request for CustomerId: {request.CustomerId}, CorrelationId: {request.CorrelationId}");
            try
            {
                await _pdfService.GenerateReportAsync(request.CustomerId, request.CorrelationId, request.StartDate, request.EndDate);
                return Ok(new { Message = "Reporte generado exitosamente." });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return StatusCode(500, new { Message = "Error al generar el reporte.", Details = exception.Message });
            }
        }
    }
}
