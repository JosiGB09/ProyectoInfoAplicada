using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerHangfire.Models;

namespace ServerHangfire.Services
{
    public interface IReportService
    {
        Task CallPdfApi(ReportRequest request);
    }

    public class ReportService : IReportService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ReportService> _logger;
        private readonly KafkaProducerService _kafka;
        private readonly IConfiguration _configuration;

        public ReportService(IHttpClientFactory httpFactory,
                             ILogger<ReportService> logger,
                             KafkaProducerService kafka,
                             IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _kafka = kafka;
            _configuration = configuration;
        }

        public async Task CallPdfApi(ReportRequest request)
        {
            try
            {
                var baseUrl = _configuration["PDFServer:BaseUrl"];
                var pdfApiUrl = $"{baseUrl}/api/PDFReports/GenerateReport";

                var client = _httpFactory.CreateClient();
                var response = await client.PostAsJsonAsync(pdfApiUrl, request);

                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/CallPdfApi",
                    Message = $"PDF API respondió con estado {response.StatusCode}",
                    Success = response.IsSuccessStatusCode
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _kafka.SendLogAsync(new LogEvent
                    {
                        CorrelationId = request.CorrelationId,
                        Endpoint = "ReportService/CallPdfApi",
                        Message = $"PDF API falló: {errorContent}",
                        Success = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CallPdfApi");
                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/CallPdfApi",
                    Message = $"Excepción: {ex.Message}",
                    Success = false
                });
            }
        }
    }
}
