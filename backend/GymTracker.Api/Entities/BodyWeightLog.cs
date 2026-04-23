namespace GymTracker.Api.Entities;

public class BodyWeightLog
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal WeightKg { get; set; }
}
