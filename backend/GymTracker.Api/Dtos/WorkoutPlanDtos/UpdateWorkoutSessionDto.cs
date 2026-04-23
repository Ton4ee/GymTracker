namespace GymTracker.Api.Dtos.WorkoutSessionDtos;

public class UpdateWorkoutSessionDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime CompletedOn { get; set; }
    public int DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public List<UpdateWorkoutSessionExerciseDto> Exercises { get; set; } = new();
}

public class UpdateWorkoutSessionExerciseDto
{
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }
}