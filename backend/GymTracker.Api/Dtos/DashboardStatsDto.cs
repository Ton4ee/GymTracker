namespace GymTracker.Api.Dtos.DashboardDtos;

public class DashboardStatsDto
{
    public int TotalExercises { get; set; }
    public int TotalWorkoutPlans { get; set; }
    public int TotalWorkoutSessions { get; set; }
    public decimal? LatestWeightKg { get; set; }
    public List<RecentWorkoutSessionDto> RecentSessions { get; set; } = new();
}

public class RecentWorkoutSessionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CompletedOn { get; set; }
    public int DurationMinutes { get; set; }
    public int ExerciseCount { get; set; }
}   