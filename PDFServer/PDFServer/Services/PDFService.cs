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
        private string storageServerUrl = "http://127.0.0.1:8000/api/storage/upload"; // URL del servicio de almacenamiento
        public PDFService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        public async Task<Document> GenerateReportAsync(int customerId, string correlationId, DateTime startDate, DateTime endDate)
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
                    page.Background("#F5F7FA");

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Reporte de Ventas").FontSize(28).Bold().FontColor("#2E86C1").AlignCenter();
                                col.Item().Text($"Cliente: {customerId}").FontSize(14).FontColor("#34495E").AlignCenter();
                                col.Item().Text($"Periodo: {startDate:yyyy-MM-dd} a {endDate:yyyy-MM-dd}").FontSize(12).FontColor("#566573").AlignCenter();
                            });
                        });
                    });

                    page.Content().PaddingVertical(20).Element(content =>
                    {
                        content.Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background("#2E86C1").Element(cell => cell.Padding(5).Text("Pedido").FontColor("#FFFFFF").FontSize(12).Bold());
                                header.Cell().Background("#2E86C1").Element(cell => cell.Padding(5).Text("Fecha").FontColor("#FFFFFF").FontSize(12).Bold());
                                header.Cell().Background("#2E86C1").Element(cell => cell.Padding(5).Text("Total").FontColor("#FFFFFF").FontSize(12).Bold().AlignRight());
                            });
                            int rowIndex = 0;
                            foreach (var order in orders)
                            {
                                var bgColor = rowIndex % 2 == 0 ? "#EBF5FB" : "#D6EAF8";
                                table.Cell().Background(bgColor).Element(cell => cell.Padding(5).Text(order.SalesOrderID.ToString()).FontSize(11));
                                table.Cell().Background(bgColor).Element(cell => cell.Padding(5).Text(order.OrderDate.ToString("yyyy-MM-dd")).FontSize(11));
                                table.Cell().Background(bgColor).Element(cell => cell.Padding(5).Text(order.TotalDue.ToString("C")).FontSize(11).AlignRight());
                                rowIndex++;
                            }
                        });
                    });

                    page.Footer().Element(footer =>
                    {
                        footer.Row(row =>
                        {
                            row.RelativeItem().Text($"Fecha de creación: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#7B7D7D").AlignRight();
                        });
                    });
                });
            });

            document.GeneratePdf(filePath);
            await CreateLog(correlationId, fileName);
            try
            {
                await UploadToStorageAsync(document, fileName, correlationId, customerId.ToString(), DateTime.Now);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al subir el archivo a almacenamiento: {ex.Message}");
                
            }
            return document;
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
        public async Task UploadToStorageAsync(Document pdfFile, string fileName, string correlationId, string clientId, DateTime generationDate)
        {
            using (var memoryStream = new MemoryStream())
            {
                pdfFile.GeneratePdf(memoryStream);
                memoryStream.Position = 0;
                using (var httpClient = new HttpClient())
                {
                    var content = new MultipartFormDataContent();
                    var fileContent = new StreamContent(memoryStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                    content.Add(fileContent, "file", fileName);
                    content.Add(new StringContent(correlationId), "correlationId");
                    content.Add(new StringContent(clientId), "clientId");
                    content.Add(new StringContent(generationDate.ToString("o")), "generationDate");
                    content.Add(new StringContent(fileName),"fileName");
                    var response = await httpClient.PostAsync(storageServerUrl, content);
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
