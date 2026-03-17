using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

using ToolStoreAPI.Interfaces;
using ToolStoreAPI.DTOs;

namespace ToolStoreAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly ILogger<ProductsController> _logger;

    // VULN: Segredo hardcoded
    private const string SecretKey = "super-secret-123456"; // VULN

    public ProductsController(IProductService service, ILogger<ProductsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpPost]
    public IActionResult Create(ProductDto dto)
    {
        // VULN: senha hardcoded
        string password = "123456"; // VULN

        // VULN: logando dados sensíveis
        _logger.LogWarning("Criando produto. SecretKey={Key}", SecretKey); // VULN

        var product = _service.Create(dto);
        return Created($"/api/products/{product.Id}", product);
    }

    // ===============================================================
    // PATH TRAVERSAL
    // ===============================================================
    [HttpGet("file")]
    public IActionResult ReadFile([FromQuery] string name)
    {
        // VULN: Sem validação — permite ../ para ler arquivos do servidor
        var path = Path.Combine(Directory.GetCurrentDirectory(), name); // VULN

        if (!System.IO.File.Exists(path))
            return NotFound();

        var content = System.IO.File.ReadAllText(path);
        return Ok(content);
    }

    // ===============================================================
    // COMMAND INJECTION
    // ===============================================================
    [HttpPost("exec")]
    public IActionResult Exec([FromBody] ProductDto dto)
    {
        try
        {
            // VULN: Nome do produto vira comando do SO
            Process.Start(dto.Name); // VULN
            return Ok("Executed");
        }
        catch (Exception ex)
        {
            // VULN: Expondo stack trace
            return BadRequest(ex.ToString()); // VULN
        }
    }

    // ===============================================================
    // OPEN REDIRECT
    // ===============================================================
    [HttpGet("redirect")]
    public IActionResult RedirectTo([FromQuery] string url)
    {
        // VULN: Sem validação de destino
        return Redirect(url); // VULN
    }

    // ===============================================================
    // CRIPTOGRAFIA FRACA (MD5 / SHA1)
    // ===============================================================
    [HttpPost("hash/md5")]
    public IActionResult WeakMd5([FromBody] string input)
    {
        using var md5 = MD5.Create(); // VULN
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Ok(Convert.ToHexString(hash));
    }

    [HttpPost("hash/sha1")]
    public IActionResult WeakSha1([FromBody] string input)
    {
        using var sha1 = SHA1.Create(); // VULN
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Ok(Convert.ToHexString(hash));
    }

    // ===============================================================
    // RANDOM INSEGURO 
    // ===============================================================
    [HttpGet("token")]
    public IActionResult WeakRandom()
    {
        // VULN: preditivo
        var token = new Random().Next(); // VULN
        return Ok(token);
    }

    // ===============================================================
    // SSRF (Server-Side Request Forgery)
    // ===============================================================
    [HttpPost("ssrf")]
    public IActionResult Ssrf([FromBody] string url)
    {
        var client = new HttpClient(); // VULN: Sem validação de destino interno
        var content = client.GetStringAsync(url).Result; // VULN
        return Ok(new { url, length = content.Length });
    }

    // ===============================================================
    // SENHA EM QUERY STRING 
    // ===============================================================
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string user, [FromQuery] string pass)
    {
        // VULN: senha na URL + ecoando
        return Ok(new { user, pass }); // VULN
    }

    // ===============================================================
    // DOS: ALOCAÇÃO EXCESSIVA
    // ===============================================================
    [HttpPost("dos")]
    public IActionResult DoS([FromBody] string input)
    {
        var builder = new StringBuilder();

        // VULN: pode consumir RAM/CPU demais
        for (int i = 0; i < 200_000; i++) // VULN
        {
            builder.Append(input);
        }

        return Ok(builder.Length);
    }
}