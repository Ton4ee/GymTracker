namespace GymTracker.Api.Entities;

public class FavoriteExercise
{
    public int Id { get; set; }

    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
