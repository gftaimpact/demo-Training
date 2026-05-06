using System;
using System.Net;
using System.Threading.Tasks;
using AuthModule.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthModule.Tests
{
    public class TemporaryCredentialsControllerTests
    {
        private readonly Mock<ILogger<TemporaryCredentialsController>> _loggerMock;

        public TemporaryCredentialsControllerTests()
        {
            _loggerMock = new Mock<ILogger<TemporaryCredentialsController>>();
        }

        private AuthDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AuthDbContext(options);
        }

        private TemporaryCredentialsController CreateController(
            AuthDbContext db,
            string adminId = "ADMIN1",
            bool mfaValid = true,
            string ip = "10.0.0.1")
        {
            var controller = new TemporaryCredentialsController(db, _loggerMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
            httpContext.Request.Headers["X-Admin-ID"] = adminId;
            httpContext.Request.Headers["X-MFA-Valid"] = mfaValid.ToString().ToLowerInvariant();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Theory]
        [InlineData("10.0.0.1", true)]
        [InlineData("192.168.1.5", true)]
        [InlineData("11.0.0.1", false)]
        [InlineData("", false)]
        public void IsCorporateIP_ShouldReturnExpectedResult(string ip, bool expected)
        {
            var result = InvokePrivateStaticMethod<bool>("IsCorporateIP", ip);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("USR-123", true)]
        [InlineData("EMP999", false)]
        [InlineData("", false)]
        public void UserExists_ShouldReturnExpectedResult(string userId, bool expected)
        {
            var result = InvokePrivateStaticMethod<bool>("UserExists", userId);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GenerateTemporaryCredentials_ShouldReturnForbidden_WhenMfaInvalidOrNonCorporateIp()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = CreateController(db, mfaValid: false, ip: "8.8.8.8");
            var request = new TemporaryCredentialsController.TempCredentialRequest { UserId = "USR-123" };

            var result = await controller.GenerateTemporaryCredentials(request);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal((int)HttpStatusCode.Forbidden, statusResult.StatusCode);

            dynamic value = statusResult.Value!;
            Assert.Equal("MFA inválido o IP no autorizada", (string)value.error);
        }

        [Fact]
        public async Task GenerateTemporaryCredentials_ShouldReturnBadRequest_WhenUserDoesNotExist()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = CreateController(db);
            var request = new TemporaryCredentialsController.TempCredentialRequest { UserId = "EMP999" };

            var result = await controller.GenerateTemporaryCredentials(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequest.Value!;
            Assert.Equal("Usuario inexistente", (string)value.error);
        }

        [Fact]
        public async Task GenerateTemporaryCredentials_ShouldReturnConflict_WhenActiveCredentialExists()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            db.TemporaryCredentials.Add(new TemporaryCredential
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "USR-123",
                AdminId = "ADMIN1",
                Status = CredentialStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                TokenEncrypted = "EXISTING_TOKEN"
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var request = new TemporaryCredentialsController.TempCredentialRequest { UserId = "USR-123" };

            var result = await controller.GenerateTemporaryCredentials(request);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            dynamic value = conflict.Value!;
            Assert.Equal("Credencial temporal ya activa", (string)value.error);
        }

        [Fact]
        public async Task GenerateTemporaryCredentials_ShouldReturnOk_WhenValidRequest()
        {
            using var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
            var controller = CreateController(db);
            var request = new TemporaryCredentialsController.TempCredentialRequest { UserId = "USR-123" };

            var result = await controller.GenerateTemporaryCredentials(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<TemporaryCredentialsController.TempCredentialResponse>(okResult.Value);

            Assert.Equal("success", response.Status);
            Assert.NotNull(response.TemporaryLink);
            Assert.StartsWith("https://auth.company.com/temp/", response.TemporaryLink);
            Assert.True(response.ExpiresAt > DateTime.UtcNow);
            Assert.StartsWith("AUD-", response.AuditId);
        }

        private static T InvokePrivateStaticMethod<T>(string methodName, params object[] parameters)
        {
            var type = typeof(TemporaryCredentialsController);
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (T)method.Invoke(null, parameters)!;
        }
    }
}
