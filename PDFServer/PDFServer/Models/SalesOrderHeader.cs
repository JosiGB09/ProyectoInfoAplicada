namespace PDFServer.Models
{
    public class SalesOrderHeader
    {
        public int SalesOrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalDue { get; set; }
    }
}
