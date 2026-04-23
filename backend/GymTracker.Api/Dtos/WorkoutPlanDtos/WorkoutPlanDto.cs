namespace GymTracker.Api.Dtos.WorkoutPlanDtos;

public class WorkoutPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkoutPlanExerciseDto> Exercises { get; set; } = new();
}

public class WorkoutPlanExerciseDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public string? Notes { get; set; }
}