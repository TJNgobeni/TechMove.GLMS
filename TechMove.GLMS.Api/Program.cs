using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TechMove.GLMS.Services;
using TechMove.GLMS.Api.Services;
using TechMove.GLMS.Data;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TechMove.GLMS.Api",
        Version = "v1",
        Description = "Contracts and service requests API for GLMS"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter into field the word 'Bearer ' followed by a valid JWT token.",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.WebHost.UseUrls("http://localhost:5001");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<TechMove.GLMS.Data.AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}
else
{
    builder.Services.AddDbContext<TechMove.GLMS.Data.AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("TechMove.GLMS.Tests");
    });
}

builder.Services.AddHttpClient<TechMove.GLMS.Services.ICurrencyService, TechMove.GLMS.Services.CurrencyService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddScoped<TechMove.GLMS.Services.ICurrencyService, TechMove.GLMS.Services.CurrencyService>();
builder.Services.AddScoped<TechMove.GLMS.Services.IValidationService, TechMove.GLMS.Services.ValidationService>();
builder.Services.AddScoped<TechMove.GLMS.Api.Services.IFileStorage, TechMove.GLMS.Api.Services.LocalFileStorage>();
builder.Services.AddScoped<TechMove.GLMS.Api.Services.IContractService, TechMove.GLMS.Api.Services.ContractService>();

// TODO: replace with real JWT auth once keys/secrets are in place.
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-please-replace";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechMove.GLMS";
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
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TechMove.GLMS.Data.AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
