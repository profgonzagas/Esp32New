using ESP32Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlite("Data Source=esp32_sensors.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ESP32 Sensor API", Version = "v1" });
});

// Permite acesso do app MAUI Android (sem HTTPS no ambiente local)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Criar banco e tabelas automaticamente na primeira execução
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SensorDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();

// Escuta em todas as interfaces da rede local na porta 5000
app.Run("http://0.0.0.0:5000");
