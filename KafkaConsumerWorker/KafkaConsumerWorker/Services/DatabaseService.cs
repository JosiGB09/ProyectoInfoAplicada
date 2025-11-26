using System;
using System.Data;
using System.Threading.Tasks;
using KafkaConsumerWorker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerWorker.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnsureTableExistsAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("No hay string de conexión configurado para DefaultConnection.");
                return;
            }

            const string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'KafkaLogs')
            BEGIN
                CREATE TABLE dbo.KafkaLogs
                (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    CorrelationId NVARCHAR(255) NULL,
                    Service NVARCHAR(255) NULL,
                    Endpoint NVARCHAR(255) NULL,
                    Timestamp DATETIME2 NOT NULL,
                    Payload NVARCHAR(MAX) NULL,
                    Success BIT NOT NULL
                )
            END
            ";

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var createCmd = new SqlCommand(createTableSql, connection);
                await createCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Tabla dbo.KafkaLogs verificada/creada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al comprobar/crear la tabla KafkaLogs.");
            }
        }

        public async Task SaveLogToSql(LogModel log)
        {
            if (log is null)
            {
                _logger.LogWarning("SaveLogToSql llamó con log nulo");
                return;
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                                   ?? _configuration["ConnectionStrings:DefaultConnection"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("No hay string de conexión");
                return;
            }

            const string insertSql = @"
            INSERT INTO dbo.KafkaLogs (CorrelationId, Service, Endpoint, Timestamp, Payload, Success)
            VALUES (@CorrelationId, @Service, @Endpoint, @Timestamp, @Payload, @Success)";

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Insert log
                await using var command = new SqlCommand(insertSql, connection);

                command.Parameters.AddWithValue("@CorrelationId", (object?)log.CorrelationId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Service", (object?)log.Service ?? DBNull.Value);
                command.Parameters.AddWithValue("@Endpoint", (object?)log.Endpoint ?? DBNull.Value);
                command.Parameters.AddWithValue("@Timestamp", log.Timestamp);
                command.Parameters.AddWithValue("@Payload", (object?)log.Payload ?? DBNull.Value);
                command.Parameters.AddWithValue("@Success", log.Success);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error almacenando log en SQL");
            }
        }
    }
}
