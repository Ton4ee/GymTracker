namespace GymTracker.Api.Dtos.WorkoutSessionDtos;

public class CreateWorkoutSessionDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime CompletedOn { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public List<CreateWorkoutSessionExerciseDto> Exercises { get; set; } = new();
}

public class CreateWorkoutSessionExerciseDto
{
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }
}