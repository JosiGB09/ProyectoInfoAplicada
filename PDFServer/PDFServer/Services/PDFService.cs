using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using PDFServer.Models;
using PDFServer.Services;
using System.Threading.Tasks;

namespace PDFServer.Services
{
    public class PDFService
    {
        private readonly DatabaseService _databaseService;
        public PDFService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        public async Task<string> GenerateReportAsync(int customerId, string correlationId, DateTime startDate, DateTime endDate)
        {
            List<SalesOrderHeader> orders = await _databaseService.GetSalesOrdersAsync(customerId, startDate, endDate);

            string folderPath = Path.Combine("wwwroot", "reports", DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string fileName = $"Report_{customerId}_{DateTime.Now:HHmmss}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Text($"Reporte - Cliente {customerId}").FontSize(20).SemiBold().AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(100);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Pedido").SemiBold();
                            header.Cell().Text("Fecha");
                            header.Cell().Text("Total").AlignRight();
                        });
                        foreach (var order in orders)
                        {
                            table.Cell().Text(order.SalesOrderID.ToString());
                            table.Cell().Text(order.OrderDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text(order.TotalDue.ToString("C")).AlignRight();
                        }
                    });
                    page.Footer().AlignRight().Text($"Fecha de creación: {DateTime.Now}");
                });
            });

            document.GeneratePdf(filePath);
            await CreateLog(correlationId,fileName);
            return filePath;
        }
        public async Task CreateLog(string correlationId, string fileName)
        {
            var kafkaService = new KafkaProducerService("localhost:9092");
            await kafkaService.SendLogAsync(new LogEvent
            {
                CorrelationId = correlationId,
                Service = "PDF Server",
                Endpoint = "/api/pdf/GenerateReport",
                FileName = fileName,
                Success = true,
            });

            
        }
    }
}
