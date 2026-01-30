using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FirstTryApi.Exceptions;

namespace FirstTryApi.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenValidData()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Register")
            .Options;

        var context = new UserContext(options);

        var passwordHasher = new PasswordHasher<User>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JWTKey"]).Returns("UneCleSecreteTresLonguePourLesTests123456789");
        var jwtService = new JwtService(configMock.Object);

        var loggerMock = new Mock<ILogger<UserService>>();

        var userService = new UserService(context, passwordHasher, jwtService, loggerMock.Object);

        var userPass = new UserPass { Username = "Messi", Password = "TheGoat!" };

        // ACT
        var result = await userService.RegisterAsync(userPass);

        // ASSERT
        Assert.NotNull(result.Token);
        Assert.Equal("Messi", result.User.Username);

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Username == "Messi");
        Assert.NotNull(userInDb);
        Assert.Equal(UserRole.Admin, userInDb!.Role);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenUsernameAlreadyExists()
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Register_Duplicate")
            .Options;
        var context = new UserContext(options);

        var passwordHasher = new PasswordHasher<User>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JWTKey"]).Returns("UneCleSecreteTresLonguePourLesTests123456789");
        var jwtService = new JwtService(configMock.Object);

        var loggerMock = new Mock<ILogger<UserService>>();
        var userService = new UserService(context, passwordHasher, jwtService, loggerMock.Object);

        await userService.RegisterAsync(new UserPass { Username = "Messi", Password = "Goat!" });

        await Assert.ThrowsAsync<GameException>(() =>
            userService.RegisterAsync(new UserPass { Username = "Messi", Password = "Goat!" })
        );
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenPasswordIsValid()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Login_Success")
            .Options;
        var context = new UserContext(options);

        var passwordHasher = new PasswordHasher<User>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JWTKey"]).Returns("UneCleSecreteTresLonguePourLesTests123456789");
        var jwtService = new JwtService(configMock.Object);

        var loggerMock = new Mock<ILogger<UserService>>();
        var userService = new UserService(context, passwordHasher, jwtService, loggerMock.Object);

        await userService.RegisterAsync(new UserPass { Username = "CR7", Password = "Sucks!" });

        var result = await userService.LoginAsync(new UserPass { Username = "CR7", Password = "Sucks!" });

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("CR7", result.User.Username);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsInvalid()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Login_BadPassword")
            .Options;
        var context = new UserContext(options);

        var passwordHasher = new PasswordHasher<User>();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JWTKey"]).Returns("UneCleSecreteTresLonguePourLesTests123456789");
        var jwtService = new JwtService(configMock.Object);

        var loggerMock = new Mock<ILogger<UserService>>();
        var userService = new UserService(context, passwordHasher, jwtService, loggerMock.Object);

        await userService.RegisterAsync(new UserPass { Username = "Messi", Password = "Goat!" });

        await Assert.ThrowsAsync<GameException>(() =>
            userService.LoginAsync(new UserPass { Username = "Messi", Password = "Sucks" })
        );
    }


}
