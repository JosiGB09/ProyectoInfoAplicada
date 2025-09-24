
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

        public ReportService(IHttpClientFactory httpFactory, ILogger<ReportService> logger, KafkaProducerService kafka)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _kafka = kafka;
        }

        public async Task CallPdfApi(ReportRequest request)
        {
            try
            {
                // Esta url hay que cambiarla a la que tiene el ServerPdf
                var pdfApiUrl = "https://localhost:5001/api/reports/";

                var client = _httpFactory.CreateClient();
              
                var response = await client.PostAsJsonAsync(pdfApiUrl, request);

                var log = $"CallPdfApi: CorrelationId={request.CorrelationId}, Status={response.StatusCode}, Time={DateTime.UtcNow}";
                _logger.LogInformation(log);
                await _kafka.SendLogAsync(log);
            }
            catch (Exception ex)
            {
                var log = $"CallPdfApi-Failed: CorrelationId={request.CorrelationId}, Error={ex.Message}";
                _logger.LogError(ex, log);
                await _kafka.SendLogAsync(log);
            }
        }
    }
}

