using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Xunit;
using FirstTryApi.Models;
using FirstTryApi.Services;

namespace FirstTryApi.Tests
{
    public class JwtServiceTests
    {
        private static JwtService CreateJwtService(string secret)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key", secret),
                    new KeyValuePair<string, string?>("Jwt:Issuer", "localhost:5000"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "localhost:5000"),
                })
                .Build();

            return new JwtService(config);
        }

        [Fact]
        public void GenerateToken_ShouldReturnJwt_WithExpectedClaims()
        {
            var jwt = CreateJwtService("RonaldoIsNowhereNearTheGoatDebate_MESSICLEARSHIM");

            var user = new User
            {
                Id = 12,
                Username = "DeleAli",
                Role = 0 
            };

            string token = jwt.GenerateToken(user);

            Assert.False(string.IsNullOrWhiteSpace(token));

            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(token);

            var nameId = parsed.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            Assert.Equal("12", nameId);

            var name = parsed.Claims.First(c => c.Type == ClaimTypes.Name).Value;
            Assert.Equal("DeleAli", name);

            var role = parsed.Claims.First(c => c.Type == ClaimTypes.Role).Value;
            Assert.Equal("User", role);
        }

        [Fact]
        public void GenerateToken_ShouldContainExpClaim()
        {
            var jwt = CreateJwtService("RonaldoIsNowhereNearTheGoatDebate_MESSICLEARSHIM!!");

            var user = new User { Id = 1, Username = "Draxler", Role = 0 };

            string token = jwt.GenerateToken(user);

            var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            var expClaim = parsed.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            Assert.NotNull(expClaim);
        }
    }
}
