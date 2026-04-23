using GymTracker.Api.Data;
using GymTracker.Api.Dtos.DashboardDtos;
using GymTracker.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    public DashboardController(
        AppDbContext context,
        ICurrentUserProfileService currentUserProfileService)
    {
        _context = context;
        _currentUserProfileService = currentUserProfileService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var latestWeight = await _context.BodyWeightLogs
            .Where(x => x.UserProfileId == profileId)
            .OrderByDescending(x => x.Date)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(cancellationToken);

        var recentSessions = await _context.WorkoutSessions
            .Include(x => x.Exercises)
            .Where(x => x.UserProfileId == profileId)
            .OrderByDescending(x => x.CompletedOn)
            .Take(5)
            .Select(x => new RecentWorkoutSessionDto
            {
                Id = x.Id,
                Name = x.Name,
                CompletedOn = x.CompletedOn,
                DurationMinutes = x.DurationMinutes,
                ExerciseCount = x.Exercises.Count
            })
            .ToListAsync(cancellationToken);

        var dto = new DashboardStatsDto
        {
            TotalExercises = await _context.Exercises.CountAsync(cancellationToken),
            TotalWorkoutPlans = await _context.WorkoutPlans.CountAsync(x => x.UserProfileId == profileId, cancellationToken),
            TotalWorkoutSessions = await _context.WorkoutSessions.CountAsync(x => x.UserProfileId == profileId, cancellationToken),
            LatestWeightKg = latestWeight,
            RecentSessions = recentSessions
        };

        return Ok(dto);
    }
}
