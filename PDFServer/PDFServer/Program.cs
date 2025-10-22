using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;
// Add services to the container.

builder.Services.AddControllers();
// Registro de DatabaseService y PDFService para inyección de dependencias
builder.Services.AddScoped<PDFServer.Services.DatabaseService>();
builder.Services.AddScoped<PDFServer.Services.PDFService>();
builder.Services.AddHttpClient();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Manejo global de excepciones no controladas
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Console.WriteLine($"[UNHANDLED EXCEPTION] {e.ExceptionObject}");
};

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
