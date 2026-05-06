using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

namespace AuthModule.Controllers
{
    [ApiController]
    [Route("api/v1/auth/temporary-credentials")]
    public class TemporaryCredentialsController : ControllerBase
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<TemporaryCredentialsController> _logger;

        public TemporaryCredentialsController(AuthDbContext db, ILogger<TemporaryCredentialsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class TempCredentialRequest
        {
            public string UserId { get; set; }
        }

        public class TempCredentialResponse
        {
            public string Status { get; set; }
            public string TemporaryLink { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string AuditId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateTemporaryCredentials([FromBody] TempCredentialRequest request)
        {
            var adminId = Request.Headers["X-Admin-ID"].FirstOrDefault();
            var mfaValid = Request.Headers["X-MFA-Valid"].FirstOrDefault() == "true";
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                // 1. Validación MFA e IP corporativa
                if (!mfaValid || !IsCorporateIP(clientIp))
                {
                    LogAudit(adminId, request.UserId, "warn", "MFA inválido o IP no autorizada");
                    return StatusCode((int)HttpStatusCode.Forbidden, new { error = "MFA inválido o IP no autorizada" });
                }

                // 2. Validación de existencia de usuario
                if (!UserExists(request.UserId))
                {
                    LogAudit(adminId, request.UserId, "warn", "Usuario inexistente");
                    return BadRequest(new { error = "Usuario inexistente" });
                }

                // 3. Control de duplicados
                var existing = await _db.TemporaryCredentials
                    .Where(c => c.UserId == request.UserId && c.Status == CredentialStatus.Active)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    LogAudit(adminId, request.UserId, "warn", "Credencial activa ya existente");
                    return Conflict(new { error = "Credencial temporal ya activa" });
                }

                // 4. Generación del token seguro
                var (token, expires) = GenerateJwtToken(request.UserId, adminId);
                var encryptedToken = EncryptTokenAES(token);

                var credential = new TemporaryCredential
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    AdminId = adminId,
                    TokenEncrypted = encryptedToken,
                    ExpiresAt = expires,
                    Status = CredentialStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _db.TemporaryCredentials.Add(credential);
                await _db.SaveChangesAsync();

                // 5. Registro auditoría
                var auditId = LogAudit(adminId, request.UserId, "success", credential.Id);

                // 6. Respuesta
                var response = new TempCredentialResponse
                {
                    Status = "success",
                    TemporaryLink = $"https://auth.company.com/temp/{credential.Id}",
                    ExpiresAt = expires,
                    AuditId = auditId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al generar credencial temporal");
                LogAudit(adminId, request.UserId, "error", ex.Message);
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        // ==========================================================
        // MÉTODOS AUXILIARES
        // ==========================================================

        private static bool IsCorporateIP(string ip)
        {
            return !string.IsNullOrEmpty(ip) && (ip.StartsWith("10.") || ip.StartsWith("192.168."));
        }

        private static bool UserExists(string userId)
        {
            // Simulación de verificación real en BD corporativa
            return userId.StartsWith("USR-");
        }

        private static (string token, DateTime expires) GenerateJwtToken(string userId, string adminId)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(System.IO.File.ReadAllText("private_key.pem"));

            var key = new RsaSecurityKey(rsa);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            var expires = DateTime.UtcNow.AddHours(24);

            var claims = new Dictionary<string, object>
            {
                { "user_id", userId },
                { "admin_id", adminId },
                { "iat", DateTime.UtcNow },
                { "exp", expires }
            };

            var jwt = new JwtSecurityToken(
                issuer: "auth.company.com",
                audience: "auth.company.com",
                claims: claims.Select(c => new System.Security.Claims.Claim(c.Key, c.Value.ToString())),
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return (token, expires);
        }

        private static string EncryptTokenAES(string token)
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encryptedBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);

            var result = Convert.ToBase64String(aes.IV.Concat(encryptedBytes).ToArray());
            return result;
        }

        private string LogAudit(string adminId, string userId, string status, string details)
        {
            var auditId = $"AUD-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId}";
            _logger.LogInformation($"[AUDIT] admin={adminId} user={userId} status={status} details={details}");
            return auditId;
        }
    }

    // ==========================================================
    // ENTIDADES Y CONTEXTO DE BASE DE DATOS
    // ==========================================================

    public enum CredentialStatus
    {
        Active,
        Revoked,
        Expired
    }

    public class TemporaryCredential
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string AdminId { get; set; }
        public string TokenEncrypted { get; set; }
        public DateTime ExpiresAt { get; set; }
        public CredentialStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<TemporaryCredential> TemporaryCredentials { get; set; }
    }
}