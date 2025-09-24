
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

        public ReportService(IHttpClientFactory httpFactory, ILogger<ReportService> logger, KafkaProducerService kafka, IConfiguration configuration)
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
                //Obtener la URL base del PDFServer
                var PDFServerBaseUrl = _configuration["PDFServer:BaseUrl"];
                var pdfApiUrl = $"{PDFServerBaseUrl}/api/PDFReports/GenerateReport";

                var client = _httpFactory.CreateClient();
              
                var response = await client.PostAsJsonAsync(pdfApiUrl, request);

                var log = $"CallPdfApi: CorrelationId={request.CorrelationId}, Status={response.StatusCode}, Time={DateTime.UtcNow}";
                _logger.LogInformation(log);
                await _kafka.SendLogAsync(log);

                if (response.IsSuccessStatusCode)
                {
                    //Éxito
                    var successLog = $"PDF API respondió exitosamente: CorrelationId={request.CorrelationId}";
                    _logger.LogInformation(successLog);
                    await _kafka.SendLogAsync(successLog);
                }
                else
                {
                    //Error
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorLog = $"PDF API falló: CorrelationId={request.CorrelationId}, Status={response.StatusCode}, Error={errorContent}";
                    _logger.LogError(errorLog);
                    await _kafka.SendLogAsync(errorLog);
                }
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

