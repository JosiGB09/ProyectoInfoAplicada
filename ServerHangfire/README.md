# Descripción del Proyecto

Servidor Hangfire en .NET 6/7 que encola solicitudes de generación de reportes (PDF) y 
ejecuta llamadas diferidas a un endpoint de generación. Incluye un placeholder seguro para Kafka.

---

## Instalación

1. Clonar el repositorio 
   
2.Instalacion de paquetes
dotnet add package Hangfire
dotnet add package Hangfire.SqlServer
dotnet add package Serilog.AspNetCore
dotnet add package Confluent.Kafka
dotnet add package Microsoft.DataSqlClient
Configurar appsettings.Development.json con tu conexión a SQL Server (LocalDB funciona): 
/aca realmente creo que no se cambia nada si ya tiene la instancia de sql server


## Pruebas en Postman

Probar localmente con Postman


Crear solicitudes POST a /api/reports con JSON, por ejemplo:

json
Copiar código
{
  "customerId": 101,
  "startDate": "2025-01-01T00:00:00",
  "endDate": "2025-01-31T23:59:59"
}
En Postman:

Método: POST

URL: ("applicationUrl": "https://localhost:7036;http://localhost:5294")

Body → raw → JSON

Enviar 3 solicitudes con distintos customerId o fechas.


Abrir Hangfire Dashboard en https://localhost:7036/hangfire y verás las tareas encoladas (Delayed jobs).

Tras el tiempo de DelayMinutes, las tareas se ejecutan y los logs aparecen en consola o Kafka (si está activo).


