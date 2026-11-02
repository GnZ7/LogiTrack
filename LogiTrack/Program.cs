using LogiTrack.Data;
using LogiTrack.Data.Seeding;
using LogiTrack.Features.Auth;
using LogiTrack.Features.Items;
using LogiTrack.Features.Orders;
using LogiTrack.Shared.ErrorHandling;
using LogiTrack.Shared.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddMemoryCache();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
        });

        return Task.CompletedTask;
    });
});

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "Db", "logitrack.db");

builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var jwtKey = builder.Configuration["Authentication:Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Authentication:Jwt:Key with a secret of at least 32 bytes using a secure configuration provider.");
}

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "LogiTrack";
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "LogiTrack";

builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LogiTrackContext>()
    .AddSignInManager();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "email",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.OperatorOrAdministrator, policy =>
        policy.RequireRole(AuthRoles.Operator, AuthRoles.Administrator));
    options.AddPolicy(AuthPolicies.Administrator, policy =>
        policy.RequireRole(AuthRoles.Administrator));
});

builder.Services.AddScoped<IdentitySeedService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<SeedService>();
}

builder.Services.AddValidation();

var app = builder.Build();

// Map endpoints
app.MapAuth();
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

await app.SeedIdentityAsync();

app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Run();