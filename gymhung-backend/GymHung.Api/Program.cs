using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using GymHung.Application;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("JWT signing key requires at least 32 bytes.");
builder.Services.AddControllers().AddJsonOptions(o => { o.JsonSerializerOptions.Converters.Add(new LongStringConverter()); o.JsonSerializerOptions.Converters.Add(new DecimalStringConverter()); });
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<GymDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("GymDatabase")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<WorkspaceScope>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<ResourceQueryService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<AiSqlContext>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PackageUsageService>();
builder.Services.AddHttpClient<IAiProvider, OllamaAiProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ai:BaseUrl"] ?? "http://127.0.0.1:2024/");
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ClockSkew = TimeSpan.FromSeconds(30) }; o.Events = new JwtBearerEvents { OnTokenValidated = async context => { if (!long.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Fail("Invalid subject"); return; } var db = context.HttpContext.RequestServices.GetRequiredService<GymDbContext>(); if (!await db.Users.AnyAsync(u => u.UserId == id && u.Status == "Active")) context.Fail("Inactive account"); } }; });
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, GymManagement.Api.DatabasePermissionHandler>();
builder.Services.AddAuthorization(o => { foreach (var code in new[] { "dashboard.read", "payments.write", "ai.write", "churn.write" }) o.AddPolicy(code, p => p.RequireAuthenticatedUser().AddRequirements(new GymManagement.Api.DatabasePermission(code))); });
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:3107", "http://127.0.0.1:3107", "http://localhost:5173", "http://127.0.0.1:5173" }).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
var app = builder.Build();
app.Use(async (context, next) => { try { await next(); } catch (Exception ex) { var status = ex switch { ApiException a => a.Status, Microsoft.EntityFrameworkCore.DbUpdateException => 409, Microsoft.Data.SqlClient.SqlException s when s.Number is 1205 or 51000 => 409, _ => 500 }; var message = ex is ApiException ? ex.Message : status == 409 ? "Dữ liệu trùng, đang được sử dụng hoặc đã thay đổi. Vui lòng tải lại." : "Không thể xử lý yêu cầu. Vui lòng thử lại."; if (status == 500) app.Logger.LogError("Unhandled request failure. TraceId={TraceId}, Type={Type}", context.TraceIdentifier, ex.GetType().Name); context.Response.StatusCode = status; await Results.Problem(statusCode: status, title: message, detail: status < 500 ? message : null, extensions: new Dictionary<string, object?> { { "traceId", context.TraceIdentifier } }).ExecuteAsync(context); } });
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); } else app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program
{
}


