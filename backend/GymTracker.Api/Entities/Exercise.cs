namespace GymTracker.Api.Entities;

public class Exercise
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? BodyPart { get; set; }
    public string? Equipment { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsEnglish { get; set; }
    public string Source { get; set; } = "wger";
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutPlanExercise> WorkoutPlanExercises { get; set; } = new List<WorkoutPlanExercise>();
    public ICollection<WorkoutSessionExercise> WorkoutSessionExercises { get; set; } = new List<WorkoutSessionExercise>();
    public ICollection<FavoriteExercise> FavoriteExercises { get; set; } = new List<FavoriteExercise>();
}