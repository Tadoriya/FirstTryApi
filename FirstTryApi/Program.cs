using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;


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
       // builder.Services.AddScoped<UserService>();
        //builder.Services.AddScoped<GameService>();
        //builder.Services.AddScoped<InventoryService>();


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
            options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors("AllowAll");
        //app.UseCors("AllowSpecificOrigin");
        app.MapControllers();


        app.Run();
    }
}
