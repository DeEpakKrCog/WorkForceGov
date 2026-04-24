using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using WorkForceGovProject.Controllers;
using WorkForceGovProject.Interfaces.Services;
using WorkForceGovProject.Models; // Assuming User model is here

namespace WorkForceGov.Citizen.API.Tests.Controllers
{
    [TestFixture]
    internal class AuthControllerTests
    {
        private Mock<IAccountService> _mockAccountService;
        private Mock<IConfiguration> _mockConfig;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockConfig = new Mock<IConfiguration>();

            // Setup Mock Configuration
            _mockConfig.Setup(c => c.GetSection("Jwt")["Key"]).Returns("super_secret_key_that_is_long_enough_32_chars");
            _mockConfig.Setup(c => c.GetSection("Jwt")["Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c.GetSection("Jwt")["Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c.GetSection("Jwt")["ExpiryHours"]).Returns("8");

            _controller = new AuthController(_mockAccountService.Object, _mockConfig.Object);
        }

        [Test]
        public async Task Login_ShouldReturnBadRequest_WhenRequestIsInvalid()
        {
            // Arrange
            var request = new LoginRequest("", "");

            // Act
            var result = await _controller.Login(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreWrong()
        {
            // Arrange
            var request = new LoginRequest("test@test", "wrongpassword");
            _mockAccountService.Setup(s => s.LoginAsync(request.Email, request.Password))
                .ReturnsAsync((false, "Invalid credentials", null));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.ToString().Should().Contain("Invalid credentials");
        }

        [Test]
        public async Task Login_ShouldReturnOkWithToken_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginRequest("admin@workforce.gov", "Password123!");
            var mockUser = new User // Adjust based on your actual User model
            {
                Id = 1,
                Email = "admin@workforce.gov",
                FullName = "Admin User",
                Role = "Administrator"
            };

            _mockAccountService.Setup(s => s.LoginAsync(request.Email, request.Password))
                .ReturnsAsync((true, "Success", mockUser));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;

            response.Token.Should().NotBeNullOrEmpty();
            response.Email.Should().Be(mockUser.Email);
            response.Role.Should().Be(mockUser.Role);

            // Verify JWT Claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(response.Token);

            jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Administrator");
            jwtToken.Claims.First(c => c.Type == "FullName").Value.Should().Be("Admin User");
            jwtToken.Issuer.Should().Be("TestIssuer");
        }

        [Test]
        public void Login_ShouldFail_IfJwtKeyIsMissing()
        {
            // Arrange
            _mockConfig.Setup(c => c.GetSection("Jwt")["Key"]).Returns((string)null);
            var request = new LoginRequest("test@test.com", "pass");
            _mockAccountService.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, "Success", new User { Id = 1, Email = "a@a.com", Role = "User", FullName = "Name" }));

            // Act & Assert
            // This expects the controller to throw an ArgumentNullException because of the ! null-forgiving operator on the Key
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _controller.Login(request));
        }
    }
}