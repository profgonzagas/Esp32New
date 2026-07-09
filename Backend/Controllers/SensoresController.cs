using ESP32Api.Data;
using ESP32Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ESP32Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensoresController : ControllerBase
{
    private readonly SensorDbContext _db;

    public SensoresController(SensorDbContext db) => _db = db;

    /// <summary>
    /// ESP32 ou o app MAUI publica uma nova leitura de sensores.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SensorReading>> Post([FromBody] SensorReading reading)
    {
        reading.Timestamp = DateTime.UtcNow;
        _db.Readings.Add(reading);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLatest), reading);
    }

    /// <summary>
    /// Retorna a leitura mais recente.
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<SensorReading>> GetLatest()
    {
        var latest = await _db.Readings
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();

        return latest is null ? NotFound("Nenhuma leitura disponível.") : Ok(latest);
    }

    /// <summary>
    /// Retorna o histórico de leituras (padrão: últimas 100).
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<SensorReading>>> GetHistory([FromQuery] int count = 100)
    {
        var data = await _db.Readings
            .OrderByDescending(r => r.Timestamp)
            .Take(Math.Clamp(count, 1, 1000))
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// Retorna leituras entre duas datas (ISO 8601).
    /// </summary>
    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<SensorReading>>> GetRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var data = await _db.Readings
            .Where(r => r.Timestamp >= from && r.Timestamp <= to)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();

        return Ok(data);
    }
}
