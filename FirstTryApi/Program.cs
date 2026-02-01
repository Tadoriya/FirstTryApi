using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using FirstTryApi.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FirstTryApi.Hubs;


namespace FirstTryApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddDbContext<UserContext>(options => options.UseSqlite("Data Source=User.db")); 
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<GameService>();
        builder.Services.AddScoped<InventoryService>();
        builder.Services.AddSingleton<ConnectionTrackerService>();



        builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(10), 
                ValidateLifetime = true, 
                ValidateIssuerSigningKey = true, 
                ValidAudience = "localhost:5000", 
                ValidIssuer = "localhost:5000", 
                IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("MessiIsTheGreatestOfAllTime8BallonDors")
                ),
                RoleClaimType = ClaimTypes.Role 
            };
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); 
            });
        });


        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR();
        builder.Services.AddRateLimiter(options =>
        {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("perUser", context =>
            {
                var username =
                    context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString();
                return RateLimitPartition.GetFixedWindowLimiter( username!,_ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(10),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                );
            });
        });
        builder.Services.AddHostedService<PassiveIncomeService>();

        var app = builder.Build();
        app.Logger.LogInformation("Application is starting up...");


        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapHub<ChatHub>("/hub/chat");
        //app.UseCors("AllowSpecificOrigin");
        app.UseRateLimiter();
        app.MapControllers();

        app.Logger.LogInformation("Application startup complete. Ready to receive requests.");

        app.Run();
    }
}
