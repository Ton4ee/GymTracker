using GymTracker.Api.Data;
using GymTracker.Api.Dtos.BodyWeightDtos;
using GymTracker.Api.Entities;
using GymTracker.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BodyWeightsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    public BodyWeightsController(
        AppDbContext context,
        ICurrentUserProfileService currentUserProfileService)
    {
        _context = context;
        _currentUserProfileService = currentUserProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BodyWeightLogDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var logs = await _context.BodyWeightLogs
            .Where(x => x.UserProfileId == profileId)
            .OrderByDescending(x => x.Date)
            .Select(x => new BodyWeightLogDto
            {
                Id = x.Id,
                Date = x.Date,
                WeightKg = x.WeightKg
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    [HttpPost]
    public async Task<ActionResult<BodyWeightLogDto>> Create(
        CreateBodyWeightLogDto dto,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        if (dto.WeightKg <= 0)
            return BadRequest("Weight must be greater than 0.");

        var log = new BodyWeightLog
        {
            UserProfileId = profileId,
            Date = dto.Date,
            WeightKg = dto.WeightKg
        };

        _context.BodyWeightLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        var result = new BodyWeightLogDto
        {
            Id = log.Id,
            Date = log.Date,
            WeightKg = log.WeightKg
        };

        return CreatedAtAction(nameof(GetAll), new { id = log.Id }, result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var log = await _context.BodyWeightLogs
            .FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profileId, cancellationToken);

        if (log == null)
            return NotFound();

        _context.BodyWeightLogs.Remove(log);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
