namespace GymTracker.Api.Entities;

public class WorkoutPlanExercise
{
    public int Id { get; set; }

    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int OrderIndex { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public string? Notes { get; set; }
}