using LogiTrack.Data;
using LogiTrack.Data.Seeding;
using LogiTrack.Features.Items;
using LogiTrack.Features.Orders;
using LogiTrack.Shared.ErrorHandling;
using LogiTrack.Shared.OpenApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "Db", "logitrack.db");

builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<SeedService>();
}

builder.Services.AddValidation();

var app = builder.Build();

// Map endpoints
app.MapItems();
app.MapOrders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{ 
    await app.MigrateDbAsync();
    await app.SeedDataAsync();
    app.ConfigureSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.Run();