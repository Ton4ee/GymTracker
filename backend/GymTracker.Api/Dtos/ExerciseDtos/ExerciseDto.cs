namespace GymTracker.Api.Dtos.ExerciseDtos;

public class ExerciseDto
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? BodyPart { get; set; }
    public string? Equipment { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsEnglish { get; set; }
    public bool IsFavorite { get; set; }
}