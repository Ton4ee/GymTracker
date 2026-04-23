using GymTracker.Api.Data;
using GymTracker.Api.Dtos.WorkoutSessionDtos;
using GymTracker.Api.Entities;
using GymTracker.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkoutSessionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    public WorkoutSessionsController(
        AppDbContext context,
        ICurrentUserProfileService currentUserProfileService)
    {
        _context = context;
        _currentUserProfileService = currentUserProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutSessionDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var sessions = await _context.WorkoutSessions
            .Include(ws => ws.Exercises)
            .ThenInclude(wse => wse.Exercise)
            .Where(ws => ws.UserProfileId == profileId)
            .OrderByDescending(ws => ws.CompletedOn)
            .Select(ws => new WorkoutSessionDto
            {
                Id = ws.Id,
                Name = ws.Name,
                CompletedOn = ws.CompletedOn,
                DurationMinutes = ws.DurationMinutes,
                Notes = ws.Notes,
                Exercises = ws.Exercises
                    .Select(e => new WorkoutSessionExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        WeightKg = e.WeightKg,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutSessionDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var session = await _context.WorkoutSessions
            .Include(ws => ws.Exercises)
            .ThenInclude(wse => wse.Exercise)
            .Where(ws => ws.Id == id && ws.UserProfileId == profileId)
            .Select(ws => new WorkoutSessionDto
            {
                Id = ws.Id,
                Name = ws.Name,
                CompletedOn = ws.CompletedOn,
                DurationMinutes = ws.DurationMinutes,
                Notes = ws.Notes,
                Exercises = ws.Exercises
                    .Select(e => new WorkoutSessionExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        WeightKg = e.WeightKg,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpPost]
    public async Task<ActionResult<WorkoutSessionDto>> Create(
        CreateWorkoutSessionDto dto,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Session name is required.");

        if (dto.Exercises.Count == 0)
            return BadRequest("At least one exercise is required.");

        var exerciseIds = dto.Exercises.Select(x => x.ExerciseId).Distinct().ToList();

        var validExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (validExerciseIds.Count != exerciseIds.Count)
            return BadRequest("One or more selected exercises are invalid.");

        var session = new WorkoutSession
        {
            UserProfileId = profileId,
            Name = dto.Name,
            CompletedOn = dto.CompletedOn,
            DurationMinutes = dto.DurationMinutes,
            Notes = dto.Notes,
            Exercises = dto.Exercises.Select(x => new WorkoutSessionExercise
            {
                ExerciseId = x.ExerciseId,
                Sets = x.Sets,
                Reps = x.Reps,
                WeightKg = x.WeightKg,
                Notes = x.Notes
            }).ToList()
        };

        _context.WorkoutSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = session.Id },
            await BuildDto(session.Id, profileId, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkoutSessionDto>> Update(
        int id,
        UpdateWorkoutSessionDto dto,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var session = await _context.WorkoutSessions
            .Include(ws => ws.Exercises)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserProfileId == profileId, cancellationToken);

        if (session == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Session name is required.");

        if (dto.Exercises.Count == 0)
            return BadRequest("At least one exercise is required.");

        var exerciseIds = dto.Exercises.Select(x => x.ExerciseId).Distinct().ToList();

        var validExerciseIds = await _context.Exercises
            .Where(e => exerciseIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (validExerciseIds.Count != exerciseIds.Count)
            return BadRequest("One or more selected exercises are invalid.");

        session.Name = dto.Name;
        session.CompletedOn = dto.CompletedOn;
        session.DurationMinutes = dto.DurationMinutes;
        session.Notes = dto.Notes;

        _context.WorkoutSessionExercises.RemoveRange(session.Exercises);

        session.Exercises = dto.Exercises.Select(x => new WorkoutSessionExercise
        {
            WorkoutSessionId = session.Id,
            ExerciseId = x.ExerciseId,
            Sets = x.Sets,
            Reps = x.Reps,
            WeightKg = x.WeightKg,
            Notes = x.Notes
        }).ToList();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(await BuildDto(session.Id, profileId, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var session = await _context.WorkoutSessions
            .Include(ws => ws.Exercises)
            .FirstOrDefaultAsync(ws => ws.Id == id && ws.UserProfileId == profileId, cancellationToken);

        if (session == null)
            return NotFound();

        _context.WorkoutSessions.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<WorkoutSessionDto> BuildDto(
        int id,
        int profileId,
        CancellationToken cancellationToken)
    {
        return await _context.WorkoutSessions
            .Include(ws => ws.Exercises)
            .ThenInclude(wse => wse.Exercise)
            .Where(ws => ws.Id == id && ws.UserProfileId == profileId)
            .Select(ws => new WorkoutSessionDto
            {
                Id = ws.Id,
                Name = ws.Name,
                CompletedOn = ws.CompletedOn,
                DurationMinutes = ws.DurationMinutes,
                Notes = ws.Notes,
                Exercises = ws.Exercises
                    .Select(e => new WorkoutSessionExerciseDto
                    {
                        Id = e.Id,
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.Exercise.Name,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        WeightKg = e.WeightKg,
                        Notes = e.Notes
                    })
                    .ToList()
            })
            .FirstAsync(cancellationToken);
    }
}
