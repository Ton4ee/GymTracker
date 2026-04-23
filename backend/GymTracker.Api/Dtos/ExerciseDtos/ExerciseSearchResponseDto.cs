namespace GymTracker.Api.Dtos.ExerciseDtos;

public class ExerciseSearchResponseDto
{
    public int TotalResults { get; set; }
    public int TotalWithImages { get; set; }
    public int TotalFavorites { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
    public List<ExerciseFacetDto> BodyParts { get; set; } = new();
    public List<ExerciseFacetDto> Equipments { get; set; } = new();
    public List<ExerciseFacetDto> Categories { get; set; } = new();
    public List<ExerciseCollectionDto> FeaturedCollections { get; set; } = new();
}

public class ExerciseFacetDto
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ExerciseCollectionDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ExerciseDto> Exercises { get; set; } = new();
}
