namespace GymTracker.Api.Entities;

public class WorkoutSessionExercise
{
    public int Id { get; set; }

    public int WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }
}