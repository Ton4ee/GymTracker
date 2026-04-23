using GymTracker.Api.Data;
using GymTracker.Api.Dtos.WorkoutPlanDtos;
using GymTracker.Api.Entities;
using GymTracker.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkoutPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    public WorkoutPlansController(
        AppDbContext context,
        ICurrentUserProfileService currentUserProfileService)
    {
        _context = context;
        _currentUserProfileService = currentUserProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutPlanDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var plans = await _context.WorkoutPlans
            .Include(wp => wp.Exercises)
            .ThenInclude(wpe => wpe.Exercise)
            .Where(wp => wp.UserProfileId == profileId)
            .OrderBy(wp => wp.Name)
            .Select(wp => new WorkoutPlanDto
            {
                Id = wp.Id,
                Name = wp.Name,
                Description = wp.Description,
                CreatedAt = wp.CreatedAt,
                Exercises = wp.Exercises
                    .OrderBy(e => e.OrderIndex)
                    .Select(e => new WorkoutPlanExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        OrderIndex = e.OrderIndex,
                        TargetSets = e.TargetSets,
                        TargetReps = e.TargetReps,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutPlanDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var plan = await _context.WorkoutPlans
            .Include(wp => wp.Exercises)
            .ThenInclude(wpe => wpe.Exercise)
            .Where(wp => wp.Id == id && wp.UserProfileId == profileId)
            .Select(wp => new WorkoutPlanDto
            {
                Id = wp.Id,
                Name = wp.Name,
                Description = wp.Description,
                CreatedAt = wp.CreatedAt,
                Exercises = wp.Exercises
                    .OrderBy(e => e.OrderIndex)
                    .Select(e => new WorkoutPlanExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        OrderIndex = e.OrderIndex,
                        TargetSets = e.TargetSets,
                        TargetReps = e.TargetReps,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == null)
            return NotFound();

        return Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<WorkoutPlanDto>> Create(
        CreateWorkoutPlanDto dto,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Plan name is required.");

        if (dto.Exercises.Count == 0)
            return BadRequest("At least one exercise is required.");

        var exerciseIds = dto.Exercises.Select(x => x.ExerciseId).Distinct().ToList();

        var validExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (validExerciseIds.Count != exerciseIds.Count)
            return BadRequest("One or more selected exercises are invalid.");

        var workoutPlan = new WorkoutPlan
        {
            UserProfileId = profileId,
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            Exercises = dto.Exercises.Select(x => new WorkoutPlanExercise
            {
                ExerciseId = x.ExerciseId,
                OrderIndex = x.OrderIndex,
                TargetSets = x.TargetSets,
                TargetReps = x.TargetReps,
                Notes = x.Notes
            }).ToList()
        };

        _context.WorkoutPlans.Add(workoutPlan);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = workoutPlan.Id },
            await BuildDto(workoutPlan.Id, profileId, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkoutPlanDto>> Update(
        int id,
        UpdateWorkoutPlanDto dto,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var plan = await _context.WorkoutPlans
            .Include(wp => wp.Exercises)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserProfileId == profileId, cancellationToken);

        if (plan == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Plan name is required.");

        if (dto.Exercises.Count == 0)
            return BadRequest("At least one exercise is required.");

        var exerciseIds = dto.Exercises.Select(x => x.ExerciseId).Distinct().ToList();

        var validExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (validExerciseIds.Count != exerciseIds.Count)
            return BadRequest("One or more selected exercises are invalid.");

        plan.Name = dto.Name;
        plan.Description = dto.Description;

        _context.WorkoutPlanExercises.RemoveRange(plan.Exercises);

        plan.Exercises = dto.Exercises.Select(x => new WorkoutPlanExercise
        {
            WorkoutPlanId = plan.Id,
            ExerciseId = x.ExerciseId,
            OrderIndex = x.OrderIndex,
            TargetSets = x.TargetSets,
            TargetReps = x.TargetReps,
            Notes = x.Notes
        }).ToList();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(await BuildDto(plan.Id, profileId, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var plan = await _context.WorkoutPlans
            .Include(wp => wp.Exercises)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserProfileId == profileId, cancellationToken);

        if (plan == null)
            return NotFound();

        _context.WorkoutPlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<WorkoutPlanDto> BuildDto(
        int id,
        int profileId,
        CancellationToken cancellationToken)
    {
        return await _context.WorkoutPlans
            .Include(wp => wp.Exercises)
            .ThenInclude(wpe => wpe.Exercise)
            .Where(wp => wp.Id == id && wp.UserProfileId == profileId)
            .Select(wp => new WorkoutPlanDto
            {
                Id = wp.Id,
                Name = wp.Name,
                Description = wp.Description,
                CreatedAt = wp.CreatedAt,
                Exercises = wp.Exercises
                    .OrderBy(e => e.OrderIndex)
                    .Select(e => new WorkoutPlanExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        OrderIndex = e.OrderIndex,
                        TargetSets = e.TargetSets,
                        TargetReps = e.TargetReps,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .FirstAsync(cancellationToken);
    }
}
