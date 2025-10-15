using Hangfire;
using ServerHangfire.Services;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration)
);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddHttpClient();


// Hangfire con SQL Server
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")) //lo del appsettings.json
);
builder.Services.AddHangfireServer();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicios propios
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ReportHandlerService>();

var app = builder.Build();


app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // dashboard de Hangfire (lo meto aca porque se levanta el proyecto en dev)
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();


app.UseAuthorization();

app.MapControllers();

app.Run();
