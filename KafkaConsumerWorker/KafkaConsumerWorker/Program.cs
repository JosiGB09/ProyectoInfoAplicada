using KafkaConsumerWorker;
using KafkaConsumerWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Comprueba que la tabla existe una vez al iniciar la aplicación
using (var scope = host.Services.CreateScope())
{
    var dbService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await dbService.EnsureTableExistsAsync();
}

await host.RunAsync();
