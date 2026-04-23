namespace GymTracker.Api.Dtos.WorkoutSessionDtos;

public class WorkoutSessionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CompletedOn { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public List<WorkoutSessionExerciseDto> Exercises { get; set; } = new();
}

public class WorkoutSessionExerciseDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }
}