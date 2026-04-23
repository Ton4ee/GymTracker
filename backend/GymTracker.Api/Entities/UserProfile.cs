namespace GymTracker.Api.Entities;

public class UserProfile
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    public ICollection<BodyWeightLog> BodyWeightLogs { get; set; } = new List<BodyWeightLog>();
    public ICollection<FavoriteExercise> FavoriteExercises { get; set; } = new List<FavoriteExercise>();
}
