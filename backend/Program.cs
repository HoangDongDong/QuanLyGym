using Microsoft.EntityFrameworkCore;
using GymManagement.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. OpenAPI / Swagger
builder.Services.AddOpenApi();

// 3. Firebird DbContext
var connectionString = builder.Configuration.GetConnectionString("FirebirdConnection") 
    ?? "User=SYSDBA;Password=masterkey;Database=D:\\QuanLyPhongGym\\data\\GYMDATA.FDB;DataSource=localhost;Port=3050;Dialect=3;Charset=UTF8;";

builder.Services.AddDbContext<GymDbContext>(options =>
{
    options.UseFirebird(connectionString);
});

// 4. CORS for React Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost" || new Uri(origin).Host == "127.0.0.1")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
