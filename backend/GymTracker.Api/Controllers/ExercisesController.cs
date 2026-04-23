using GymTracker.Api.Dtos.ExerciseDtos;
using GymTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly ExerciseQueryService _exerciseQueryService;
    private readonly ExerciseImportService _exerciseImportService;

    public ExercisesController(
        ExerciseQueryService exerciseQueryService,
        ExerciseImportService exerciseImportService)
    {
        _exerciseQueryService = exerciseQueryService;
        _exerciseImportService = exerciseImportService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? bodyPart,
        [FromQuery] string? equipment,
        [FromQuery] string? category,
        [FromQuery] bool onlyWithImages = false,
        [FromQuery] bool favoritesOnly = false,
        [FromQuery] bool englishOnly = true,
        CancellationToken cancellationToken = default)
    {
        var exercises = await _exerciseQueryService.GetExercisesAsync(
            search,
            bodyPart,
            equipment,
            category,
            onlyWithImages,
            favoritesOnly,
            englishOnly,
            cancellationToken);

        return Ok(exercises);
    }

    [HttpGet("discover")]
    public async Task<ActionResult<ExerciseSearchResponseDto>> Discover(
        [FromQuery] string? search,
        [FromQuery] string? bodyPart,
        [FromQuery] string? equipment,
        [FromQuery] string? category,
        [FromQuery] bool onlyWithImages = false,
        [FromQuery] bool favoritesOnly = false,
        [FromQuery] bool englishOnly = true,
        CancellationToken cancellationToken = default)
    {
        var response = await _exerciseQueryService.DiscoverAsync(
            search,
            bodyPart,
            equipment,
            category,
            onlyWithImages,
            favoritesOnly,
            englishOnly,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var exercise = await _exerciseQueryService.GetByIdAsync(id, cancellationToken);

        if (exercise == null)
            return NotFound();

        return Ok(exercise);
    }

    [HttpGet("{id:int}/related")]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetRelated(
        int id,
        [FromQuery] int take = 4,
        CancellationToken cancellationToken = default)
    {
        var exercise = await _exerciseQueryService.GetByIdAsync(id, cancellationToken);

        if (exercise == null)
        {
            return NotFound();
        }

        var relatedExercises = await _exerciseQueryService.GetRelatedExercisesAsync(id, take, cancellationToken);
        return Ok(relatedExercises);
    }

    [HttpGet("favorites")]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetFavorites(CancellationToken cancellationToken = default)
    {
        var favorites = await _exerciseQueryService.GetFavoriteExercisesAsync(cancellationToken);
        return Ok(favorites);
    }

    [HttpPost("{id:int}/favorite")]
    public async Task<IActionResult> AddFavorite(int id, CancellationToken cancellationToken = default)
    {
        var updated = await _exerciseQueryService.SetFavoriteAsync(id, isFavorite: true, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}/favorite")]
    public async Task<IActionResult> RemoveFavorite(int id, CancellationToken cancellationToken = default)
    {
        var updated = await _exerciseQueryService.SetFavoriteAsync(id, isFavorite: false, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpPost("sync")]
    public async Task<ActionResult> SyncExercises()
    {
        var imported = await _exerciseImportService.SyncExercisesFromWgerAsync();

        return Ok(new
        {
            message = "Exercise sync completed.",
            imported
        });
    }
}