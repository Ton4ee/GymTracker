namespace GymTracker.Api.Entities;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateTime CompletedOn { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }

    public ICollection<WorkoutSessionExercise> Exercises { get; set; } = new List<WorkoutSessionExercise>();
}
