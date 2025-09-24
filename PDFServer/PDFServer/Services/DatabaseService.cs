using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PDFServer.Models;
using System.Data;

namespace PDFServer.Services
{
    public class DatabaseService
    {
        private readonly string stringConnection;

        public DatabaseService(IConfiguration configuration)
        {
            stringConnection = configuration.GetConnectionString("DefaultConnection");
        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(stringConnection);
        }
        public async Task<List<SalesOrderHeader>> GetSalesOrdersAsync(int customerId, DateTime startDate, DateTime endDate)
        {
            var orders = new List<SalesOrderHeader>();
            try
            {
                Console.WriteLine($"[GetSalesOrdersAsync] Parámetros: CustomerId={customerId}, StartDate={startDate:O}, EndDate={endDate:O}");
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    string query = @"SELECT SalesOrderID, OrderDate, TotalDue 
                                     FROM Sales.SalesOrderHeader 
                                     WHERE CustomerID = @CustomerId 
                                     AND OrderDate >= @StartDate AND OrderDate < @EndDate";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
                        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var order = new SalesOrderHeader
                                {
                                    SalesOrderID = reader.GetInt32(0),
                                    OrderDate = reader.GetDateTime(1),
                                    TotalDue = reader.GetDecimal(2)
                                };
                                orders.Add(order);
                                Console.WriteLine($"[GetSalesOrdersAsync] Registro: SalesOrderID={order.SalesOrderID}, OrderDate={order.OrderDate:O}, TotalDue={order.TotalDue}");
                            }
                        }
                    }
                }
                Console.WriteLine($"[GetSalesOrdersAsync] Total registros encontrados: {orders.Count}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"[GetSalesOrdersAsync] SqlException: {ex.Message}");
                throw new Exception(ex.Number.ToString(), ex);
            }
            return orders;
        }
    }
}
