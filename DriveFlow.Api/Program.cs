using DriveFlow.Api.Data;
using DriveFlow.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Database
builder.Services.AddDbContext<DriveFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Controllers + OpenAPI + clean error pages
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// 3) CORS: open to everyone (simple setup, no login).
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 4) Services (one DbContext )
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();                  // /openapi/v1.json 
app.UseCors();
app.MapControllers();   // Map all controllers to their routes (e.g., /api/customers, /api/vehicles, etc.)

app.MapGet("/", () => "DriveFlow API is running. OpenAPI: /openapi/v1.json");
app.Run();
