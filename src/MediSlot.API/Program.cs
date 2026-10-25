using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using MediSlot.API.Filters;
using MediSlot.API.Middleware;
using MediSlot.API.OpenApi;
using MediSlot.Application.Common;
using MediSlot.Application.Interfaces;
using MediSlot.Application.Validators;
using MediSlot.Infrastructure.Data;
using MediSlot.Infrastructure.Jobs;
using MediSlot.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT settings (appsettings.json + User Secrets madhun yetat)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or too short. Set it using User Secrets.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISpecializationService, SpecializationService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.Configure<ClinicSettings>(builder.Configuration.GetSection(ClinicSettings.SectionName));
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection(AdminSettings.SectionName));
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<INotificationService, LogNotificationService>();
builder.Services.AddScoped<AppointmentReminderJob>();


builder.Services.AddScoped<SlotGenerationJob>();

// Hangfire: jobs SQL Server madhe save hotat (HangFire naavacha vegla schema, aapli tables tashich rahtat)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

builder.Services.AddHangfireServer();

// Validation + error handling
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())); builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
}

// Recurring job register (job aadhich asel tar update hoto, duplicate nahi)
var clinicSettings = builder.Configuration.GetSection(ClinicSettings.SectionName).Get<ClinicSettings>()
    ?? new ClinicSettings();
var slotCron = builder.Configuration["Jobs:SlotGenerationCron"] ?? "0 1 * * *";

app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<SlotGenerationJob>(
    "generate-slots",
    job => job.RunAsync(),
    slotCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicSettings.TimeZoneId) });


var reminderCron = builder.Configuration["Jobs:ReminderCron"] ?? "0 * * * *";

app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<AppointmentReminderJob>(
    "send-reminders",
    job => job.RunAsync(),
    reminderCron,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(clinicSettings.TimeZoneId) });

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    // Hangfire dashboard che polling requests log karu naka (noise), pan error asel tar log kara
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null || httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;

        return httpContext.Request.Path.StartsWithSegments("/hangfire")
            ? Serilog.Events.LogEventLevel.Verbose
            : Serilog.Events.LogEventLevel.Information;
    };
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "MediSlot API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();   // aadhi: tumhi kon ahat
app.UseAuthorization();    // mag: tumhala allowed ahe ka


// Dashboard: default ne fakt local machine var ughadto (production madhe swatacha auth filter lagto)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new LocalRequestsOnlyAuthorizationFilter() }
});

app.MapControllers();

app.Run();