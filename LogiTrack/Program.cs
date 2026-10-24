using LogiTrack.Data;
using LogiTrack.Data.Seeding;
using LogiTrack.Shared.ErrorHandling;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await app.MigrateDbAsync();

    await app.SeedDataAsync();

    //TODO: Extract to Extensions
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "LogiTrack API Documentation";
        options.SwaggerEndpoint("/openapi/v1.json", "LogiTrack API V1");        
    });
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.UseHttpsRedirection();

app.Run();