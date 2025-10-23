using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerHangfire.Models;
using System.Net.Http.Json;

namespace ServerHangfire.Services
{
    public interface IReportService
    {
        Task CallPdfApi(ReportRequest request);
        Task SendEmailNotification(ReportRequest request);
        Task SendMessagingNotification(ReportRequest request);
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
                int delaySeconds = _configuration.GetValue<int?>("Hangfire:NotificationDelaySeconds") ?? 45;

                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Correlation-ID", request.CorrelationId);

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
                    return;
                }

                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/CallPdfApi",
                    Message = $"Tareas de notificación encoladas con retraso de {delaySeconds}s.",
                    Success = true
                });
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

        public async Task SendEmailNotification(ReportRequest request)
        {
            try
            {
                var baseUrl = _configuration["EmailServer:BaseUrl"];
                var emailApiUrl = $"{baseUrl}/api/email/send";

                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Correlation-ID", request.CorrelationId);

                var payload = new
                {
                    CorrelationId = request.CorrelationId,
                    to_email = "josiasmg761@gmail.com",
                    Subject = "Reporte",
                    Body = "Tu reporte PDF ha sido generado exitosamente.",
                };

                var response = await client.PostAsJsonAsync(emailApiUrl, payload);

                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/SendEmailNotification",
                    Message = $"Email Server respondió con {response.StatusCode}",
                    Success = response.IsSuccessStatusCode
                });
            }
            catch (Exception ex)
            {
                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/SendEmailNotification",
                    Message = $"Error enviando email: {ex.Message}",
                    Success = false
                });
            }
        }

        public async Task SendMessagingNotification(ReportRequest request)
        {
            try
            {
                var baseUrl = _configuration["MessagingServer:BaseUrl"];
                var msgApiUrl = $"{baseUrl}/api/messaging/send";

                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-Correlation-ID", request.CorrelationId);

                var payload = new
                {
                    Platform = "discord",
                    Recipient = "1430430727102660683",
                    Message = "Tu reporte PDF ya está disponible para descarga.",
                    CorrelationId = request.CorrelationId
                };

                var response = await client.PostAsJsonAsync(msgApiUrl, payload);

                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/SendMessagingNotification",
                    Message = $"Messaging Server respondió con {response.StatusCode}",
                    Success = response.IsSuccessStatusCode
                });
            }
            catch (Exception ex)
            {
                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = request.CorrelationId,
                    Endpoint = "ReportService/SendMessagingNotification",
                    Message = $"Error enviando mensaje: {ex.Message}",
                    Success = false
                });
            }
        }
    }
}

