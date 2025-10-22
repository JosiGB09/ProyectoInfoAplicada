using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerHangfire.Models;

namespace ServerHangfire.Services
{
    public class ReportHandlerService
    {
        private readonly ILogger<ReportHandlerService> _logger;
        private readonly KafkaProducerService _kafka;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IConfiguration _configuration;

        public ReportHandlerService(ILogger<ReportHandlerService> logger,
                                    KafkaProducerService kafka,
                                    IBackgroundJobClient backgroundJobs,
                                    IConfiguration configuration)
        {
            _logger = logger;
            _kafka = kafka;
            _backgroundJobs = backgroundJobs;
            _configuration = configuration;
        }

        /// Valida los datos de la solicitud antes de procesarla.
        public bool ValidateRequest(ReportRequest request, out string? errorMessage)
        {
            if (request == null)
            {
                errorMessage = "Request body is required.";
                return false;
            }

            if (request.CustomerId <= 0)
            {
                errorMessage = "CustomerId inválido.";
                return false;
            }

            if (request.StartDate >= request.EndDate)
            {
                errorMessage = "Rango de fechas inválido.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// Registra en logs la solicitud y envía el mensaje a Kafka
        public async Task<string> LogRequestAsync(ReportRequest request)
        {
            if (string.IsNullOrEmpty(request.CorrelationId))
                request.CorrelationId = Guid.NewGuid().ToString();

            var now = DateTime.UtcNow;
            var logMsg = $"Solicitud recibida: CorrelationId={request.CorrelationId}, " +
                         $"CustomerId={request.CustomerId}, Rango={request.StartDate:O} - {request.EndDate:O}, Hora={now:O}";

            _logger.LogInformation(logMsg);

            await _kafka.SendLogAsync(new LogEvent
            {
                CorrelationId = request.CorrelationId,
                Endpoint = "Reports/CreateReport",
                Message = "Solicitud recibida correctamente.",
                Success = true
            });

            return request.CorrelationId;
        }

 
        /// Encola la llamada a la API PDF usando Hangfire 
        public void ScheduleReportJob(ReportRequest request)
        {
            int delay = _configuration.GetValue<int?>("Hangfire:DelayMinutes") ?? 5;

            _backgroundJobs.Schedule<IReportService>(
                service => service.CallPdfApi(request),
                TimeSpan.FromMinutes(delay)
            );

            _logger.LogInformation($"Reporte encolado con un retraso de {delay} minutos (CorrelationId={request.CorrelationId}).");
        }
        /// Maneja la respuesta del PDF Server (callback)
        public async Task HandlePdfResponseAsync(PdfResponseMessage pdfResponse)
        {
            try
            {
                if (pdfResponse.Mensaje.Trim().Equals("Success", StringComparison.OrdinalIgnoreCase))
                {
                    int delay = _configuration.GetValue<int?>("Hangfire:DelayMinutes") ?? 1;
                    _logger.LogInformation("Callback realizado con exito");
                    _backgroundJobs.Schedule<IReportService>(
                        service => service.SendEmailNotification(new ReportRequest
                        {
                            CorrelationId = pdfResponse.CorrelationId
                        }),
                        TimeSpan.FromSeconds(delay)
                    );

                    _backgroundJobs.Schedule<IReportService>(
                        service => service.SendMessagingNotification(new ReportRequest
                        {
                            CorrelationId = pdfResponse.CorrelationId
                        }),
                        TimeSpan.FromSeconds(delay)
                    );

                    _logger.LogInformation($"[PDF Callback] Éxito confirmado. Jobs de notificación encolados (CorrelationId={pdfResponse.CorrelationId}).");

                }
                else
                {
                    _logger.LogWarning($"[PDF Callback] PDF falló o mensaje inválido: {pdfResponse.Mensaje} (CorrelationId={pdfResponse.CorrelationId})");

                    await _kafka.SendLogAsync(new LogEvent
                    {
                        CorrelationId = pdfResponse.CorrelationId,
                        Endpoint = "Reports/PdfCallback",
                        Message = $"PDF Server devolvió mensaje: {pdfResponse.Mensaje}",
                        Success = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al manejar respuesta del PDF Server.");

                await _kafka.SendLogAsync(new LogEvent
                {
                    CorrelationId = pdfResponse.CorrelationId,
                    Endpoint = "Reports/PdfCallback",
                    Message = $"Excepción en callback: {ex.Message}",
                    Success = false
                });
            }
        }

    }
}
