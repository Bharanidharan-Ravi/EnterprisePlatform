using System;
using System.Threading.Tasks;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIPlatform.Playground.Controllers;

[ApiController]
[Route("api/database")]
public class DatabaseValidationController : ControllerBase
{
    private readonly PlaygroundValidationService _validationService;

    public DatabaseValidationController(PlaygroundValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] PlaygroundRecord record)
    {
        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.NewGuid();
        }
        
        if (record.CreatedOn == default)
        {
            record.CreatedOn = DateTimeOffset.UtcNow;
        }

        await _validationService.CreateAsync(record);
        return CreatedAtAction(nameof(Get), new { id = record.Id }, record);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var record = await _validationService.GetAsync(id);
        if (record == null)
        {
            return NotFound();
        }
        return Ok(record);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _validationService.GetAllAsync();
        return Ok(records);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlaygroundRecord record)
    {
        if (id != record.Id)
        {
            return BadRequest("ID mismatch");
        }

        var existing = await _validationService.GetAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _validationService.UpdateAsync(record);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _validationService.GetAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        await _validationService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("count")]
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public async Task<IActionResult> Count()
    {
        var count = await _validationService.GetCountAsync();
        return Ok(new { Count = count });
    }
}
