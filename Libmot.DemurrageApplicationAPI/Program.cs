using Libmot.DemurrageApplication.Data;
using LibmotExpress.DemurrageApi.Models;
using Libmot.DemurrageApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Libmot.DemurrageApplicationAPI.Configurations;
using Libmot.DemurrageApplicationAPI.Services;
using Libmot.DemurrageApplicationAPI.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DemurrageAppConnectionString")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IDemurrageService, DemurrageService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<DemurrageAccrualJob>();
//builder.Services.AddScoped<HangfireJobScheduler>();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Adding JWT Authentication 
var jwt = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);

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
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Addsing Authorization Policies 
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("FinanceOrAdmin", p => p.RequireRole("SuperAdmin", "FinanceBillingOfficer"));
    options.AddPolicy("AllStaff", p => p.RequireRole("SuperAdmin", "FinanceBillingOfficer", "DriverDispatch"));
});

// Adding AutoMapper Services for conversion of Domain Models
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


// Adding Hangfire to handle Background Jobs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DemurrageAppConnectionString"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

// Register Jobs
builder.Services.AddScoped<DemurrageAccrualJob>();

// Registering custom scheduler service
builder.Services.AddScoped<IJobSchedulerService, JobSchedulerService>();

// Adding Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Libmot Express — Demurrage API",
        Version = "v1",
        Description = "Enterprise Demurrage Management System for Libmot Express"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Enable Hangfire Dashboard
app.UseHangfireDashboard();

// Registering midnight WAT recurring job 
using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate<DemurrageAccrualJob>(
        recurringJobId: "daily-demurrage-accrual",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: "0 23 * * *",   // 23:00 UTC = 00:00 WAT (UTC+1)
        options: new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });
};


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Authentication before Authorization
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();


app.Run();
