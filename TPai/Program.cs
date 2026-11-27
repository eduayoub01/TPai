using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TPai.Data;
using TPai.Models;
using TPai.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is missing. Configure SQL Server (LocalDB by default) in appsettings.json.");
}

builder.Services.AddDbContext<RentCarDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 5;
})
.AddEntityFrameworkStores<RentCarDbContext>()
.AddDefaultTokenProviders();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short. It must be at least 32 characters long.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<ICarCategoryRepository, CarCategoryRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // The Vite dev server may run on any port and can be reached through
            // either localhost or 127.0.0.1, so accept any loopback origin in dev.
            policy.SetIsOriginAllowed(origin =>
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            if (configuredOrigins.Length > 0)
            {
                policy.WithOrigins(configuredOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

var app = builder.Build();

// Create/upgrade the database before serving requests so a fresh clone works
// without manually running `dotnet ef database update`.
try
{
    await DbInitializer.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex,
        "Database initialization failed. Check that SQL Server LocalDB is available and that " +
        "ConnectionStrings:DefaultConnection is correct, then start the API again.");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// CORS must run before the exception handler so error responses also get the
// CORS headers the browser needs to read them.
app.UseCors("Spa");

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = feature?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("GlobalExceptionHandler");

    logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
        context.Request.Method, context.Request.Path);

    var (status, title) = exception switch
    {
        DbUpdateException => (StatusCodes.Status409Conflict,
            "The requested change conflicts with existing data."),
        _ => (StatusCodes.Status500InternalServerError,
            "An unexpected error occurred while processing the request.")
    };

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
        Instance = context.Request.Path
    };
    problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

    await context.Response.WriteAsJsonAsync(problem);
}));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Small diagnostics endpoints: opening the API root no longer looks like a
// broken "404" page when the backend is the only thing running.
app.MapGet("/", () => Results.Ok(new
{
    name = "RENTIGO API",
    status = "running",
    swagger = "/swagger",
    health = "/api/health"
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTime.UtcNow
}));

app.Run();
