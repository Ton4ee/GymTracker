namespace GymTracker.Api.Dtos.WorkoutPlanDtos;

public class UpdateWorkoutPlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<UpdateWorkoutPlanExerciseDto> Exercises { get; set; } = new();
}

public class UpdateWorkoutPlanExerciseDto
{
    public int ExerciseId { get; set; }
    public int OrderIndex { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public string? Notes { get; set; }
}