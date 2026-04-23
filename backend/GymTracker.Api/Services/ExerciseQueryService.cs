using System.Linq.Expressions;
using GymTracker.Api.Data;
using GymTracker.Api.Dtos.ExerciseDtos;
using GymTracker.Api.Entities;
using GymTracker.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Services;

public class ExerciseQueryService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserProfileService _currentUserProfileService;

    private static readonly Expression<Func<Exercise, ExerciseListItem>> ExerciseListProjection = exercise => new ExerciseListItem
    {
        Id = exercise.Id,
        ExternalId = exercise.ExternalId,
        Name = exercise.Name,
        Description = exercise.Description,
        Category = exercise.Category,
        BodyPart = exercise.BodyPart,
        Equipment = exercise.Equipment,
        ImageUrl = exercise.ImageUrl,
        VideoUrl = exercise.VideoUrl,
        IsEnglish = exercise.IsEnglish
    };

    public ExerciseQueryService(
        AppDbContext context,
        ICurrentUserProfileService currentUserProfileService)
    {
        _context = context;
        _currentUserProfileService = currentUserProfileService;
    }

    public async Task<List<ExerciseDto>> GetExercisesAsync(
        string? search = null,
        string? bodyPart = null,
        string? equipment = null,
        string? category = null,
        bool onlyWithImages = false,
        bool favoritesOnly = false,
        bool englishOnly = true,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var filteredExercises = await ApplyFilters(
                BuildSearchQuery(search),
                profileId,
                bodyPart,
                equipment,
                category,
                onlyWithImages,
                favoritesOnly,
                englishOnly)
            .Select(ExerciseListProjection)
            .ToListAsync(cancellationToken);

        var favoriteIds = await GetFavoriteExerciseIdsAsync(
            profileId,
            filteredExercises.Select(exercise => exercise.Id),
            cancellationToken);

        return OrderExercises(filteredExercises, search)
            .Select(exercise => MapToDto(exercise, favoriteIds))
            .ToList();
    }

    public async Task<ExerciseSearchResponseDto> DiscoverAsync(
        string? search = null,
        string? bodyPart = null,
        string? equipment = null,
        string? category = null,
        bool onlyWithImages = false,
        bool favoritesOnly = false,
        bool englishOnly = true,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var baseQuery = ApplyFilters(
            BuildSearchQuery(search),
            profileId,
            bodyPart: null,
            equipment: null,
            category: null,
            onlyWithImages,
            favoritesOnly,
            englishOnly);

        var filteredExercises = await ApplyFilters(
                baseQuery,
                profileId,
                bodyPart,
                equipment,
                category,
                onlyWithImages: false,
                favoritesOnly: false,
                englishOnly: false)
            .Select(ExerciseListProjection)
            .ToListAsync(cancellationToken);

        var orderedExercises = OrderExercises(filteredExercises, search);

        var favoriteIds = await GetFavoriteExerciseIdsAsync(
            profileId,
            orderedExercises.Select(exercise => exercise.Id),
            cancellationToken);

        var facetSource = await baseQuery
            .Select(ExerciseListProjection)
            .ToListAsync(cancellationToken);

        return new ExerciseSearchResponseDto
        {
            TotalResults = orderedExercises.Count,
            TotalWithImages = orderedExercises.Count(HasImage),
            TotalFavorites = orderedExercises.Count(exercise => favoriteIds.Contains(exercise.Id)),
            Exercises = orderedExercises.Select(exercise => MapToDto(exercise, favoriteIds)).ToList(),
            BodyParts = BuildFacets(facetSource.Select(item => item.BodyPart)),
            Equipments = BuildFacets(facetSource.Select(item => item.Equipment)),
            Categories = BuildFacets(facetSource.Select(item => item.Category)),
            FeaturedCollections = BuildFeaturedCollections(orderedExercises, favoriteIds, search)
        };
    }

    public async Task<ExerciseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var exercise = await _context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.Id == id)
            .Select(exercise => new ExerciseDto
            {
                Id = exercise.Id,
                ExternalId = exercise.ExternalId,
                Name = exercise.Name,
                Description = exercise.Description,
                Category = exercise.Category,
                BodyPart = exercise.BodyPart,
                Equipment = exercise.Equipment,
                ImageUrl = exercise.ImageUrl,
                VideoUrl = exercise.VideoUrl,
                IsEnglish = exercise.IsEnglish
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (exercise == null)
        {
            return null;
        }

        exercise.IsFavorite = await _context.FavoriteExercises
            .AsNoTracking()
            .AnyAsync(
                favorite => favorite.UserProfileId == profileId && favorite.ExerciseId == exercise.Id,
                cancellationToken);

        return exercise;
    }

    public async Task<List<ExerciseDto>> GetRelatedExercisesAsync(
        int id,
        int take = 4,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 8);
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var currentExercise = await _context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.Id == id)
            .Select(ExerciseListProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentExercise == null)
        {
            return [];
        }

        var candidateExercises = await _context.Exercises
            .AsNoTracking()
            .Where(exercise => exercise.Id != id)
            .Where(exercise => exercise.IsEnglish)
            .Select(ExerciseListProjection)
            .ToListAsync(cancellationToken);

        var favoriteIds = await GetFavoriteExerciseIdsAsync(
            profileId,
            candidateExercises.Select(exercise => exercise.Id),
            cancellationToken);

        return candidateExercises
            .Select(candidate => new
            {
                Exercise = candidate,
                Score = CalculateRelatedScore(currentExercise, candidate)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Exercise.Name)
            .Take(take)
            .Select(item => MapToDto(item.Exercise, favoriteIds))
            .ToList();
    }

    public async Task<List<ExerciseDto>> GetFavoriteExercisesAsync(CancellationToken cancellationToken = default)
    {
        return await GetExercisesAsync(favoritesOnly: true, englishOnly: true, cancellationToken: cancellationToken);
    }

    public async Task<bool> SetFavoriteAsync(
        int exerciseId,
        bool isFavorite,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _currentUserProfileService.GetCurrentProfileIdAsync(cancellationToken);

        var exerciseExists = await _context.Exercises
            .AsNoTracking()
            .AnyAsync(exercise => exercise.Id == exerciseId, cancellationToken);

        if (!exerciseExists)
        {
            return false;
        }

        var favorite = await _context.FavoriteExercises
            .FirstOrDefaultAsync(
                item => item.UserProfileId == profileId && item.ExerciseId == exerciseId,
                cancellationToken);

        if (isFavorite && favorite == null)
        {
            _context.FavoriteExercises.Add(new FavoriteExercise
            {
                UserProfileId = profileId,
                ExerciseId = exerciseId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (!isFavorite && favorite != null)
        {
            _context.FavoriteExercises.Remove(favorite);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Exercise> BuildSearchQuery(string? search)
    {
        var query = _context.Exercises.AsNoTracking().AsQueryable();
        var searchTerms = SplitSearchTerms(search);

        foreach (var term in searchTerms)
        {
            var pattern = $"%{term}%";

            query = query.Where(exercise =>
                EF.Functions.ILike(exercise.Name, pattern) ||
                (exercise.BodyPart != null && EF.Functions.ILike(exercise.BodyPart, pattern)) ||
                (exercise.Category != null && EF.Functions.ILike(exercise.Category, pattern)) ||
                (exercise.Equipment != null && EF.Functions.ILike(exercise.Equipment, pattern)) ||
                (exercise.Description != null && EF.Functions.ILike(exercise.Description, pattern)));
        }

        return query;
    }

    private IQueryable<Exercise> ApplyFilters(
        IQueryable<Exercise> query,
        int profileId,
        string? bodyPart,
        string? equipment,
        string? category,
        bool onlyWithImages,
        bool favoritesOnly,
        bool englishOnly)
    {
        if (englishOnly)
        {
            query = query.Where(exercise => exercise.IsEnglish);
        }

        var normalizedBodyPart = NormalizeFilterValue(bodyPart);
        if (normalizedBodyPart != null)
        {
            query = query.Where(exercise => exercise.BodyPart != null && exercise.BodyPart.ToLower() == normalizedBodyPart);
        }

        var normalizedEquipment = NormalizeFilterValue(equipment);
        if (normalizedEquipment != null)
        {
            query = query.Where(exercise => exercise.Equipment != null && exercise.Equipment.ToLower() == normalizedEquipment);
        }

        var normalizedCategory = NormalizeFilterValue(category);
        if (normalizedCategory != null)
        {
            query = query.Where(exercise => exercise.Category != null && exercise.Category.ToLower() == normalizedCategory);
        }

        if (onlyWithImages)
        {
            query = query.Where(exercise => exercise.ImageUrl != null && exercise.ImageUrl != string.Empty);
        }

        if (favoritesOnly)
        {
            query = query.Where(exercise => _context.FavoriteExercises.Any(
                favorite => favorite.UserProfileId == profileId && favorite.ExerciseId == exercise.Id));
        }

        return query;
    }

    private static List<ExerciseListItem> OrderExercises(IEnumerable<ExerciseListItem> exercises, string? search)
    {
        var orderingContext = CreateOrderingContext(search);

        return exercises
            .OrderBy(exercise => CalculateSearchScore(exercise, orderingContext))
            .ThenByDescending(exercise => exercise.IsEnglish)
            .ThenBy(exercise => exercise.Name)
            .ToList();
    }

    private static int CalculateSearchScore(ExerciseListItem exercise, SearchOrderingContext orderingContext)
    {
        var normalizedSearch = orderingContext.NormalizedSearch;

        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            if (HasImage(exercise) && exercise.IsEnglish)
            {
                return 0;
            }

            if (exercise.IsEnglish)
            {
                return 1;
            }

            return HasImage(exercise) ? 2 : 3;
        }

        var normalizedName = Normalize(exercise.Name);
        var normalizedBodyPart = Normalize(exercise.BodyPart);
        var normalizedCategory = Normalize(exercise.Category);
        var normalizedEquipment = Normalize(exercise.Equipment);
        var normalizedDescription = Normalize(exercise.Description);
        var terms = orderingContext.Terms;

        if (normalizedName == normalizedSearch)
        {
            return 0;
        }

        if (normalizedName.StartsWith(normalizedSearch, StringComparison.Ordinal))
        {
            return 1;
        }

        if (terms.Length > 0 && terms.All(term => normalizedName.Contains(term, StringComparison.Ordinal)))
        {
            return 2;
        }

        if (terms.Length > 0 && terms.Any(term =>
                normalizedBodyPart.Contains(term, StringComparison.Ordinal) ||
                normalizedCategory.Contains(term, StringComparison.Ordinal) ||
                normalizedEquipment.Contains(term, StringComparison.Ordinal)))
        {
            return 3;
        }

        if (terms.Length > 0 && terms.Any(term => normalizedDescription.Contains(term, StringComparison.Ordinal)))
        {
            return 4;
        }

        if (exercise.IsEnglish && HasImage(exercise))
        {
            return 5;
        }

        if (exercise.IsEnglish)
        {
            return 6;
        }

        return HasImage(exercise) ? 7 : 8;
    }

    private static int CalculateRelatedScore(ExerciseListItem current, ExerciseListItem candidate)
    {
        var score = 0;

        if (Matches(current.BodyPart, candidate.BodyPart))
        {
            score += 4;
        }

        if (Matches(current.Category, candidate.Category))
        {
            score += 3;
        }

        if (Matches(current.Equipment, candidate.Equipment))
        {
            score += 2;
        }

        if (HasImage(candidate))
        {
            score += 1;
        }

        if (candidate.IsEnglish)
        {
            score += 1;
        }

        return score;
    }

    private static List<ExerciseFacetDto> BuildFacets(IEnumerable<string?> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ExerciseFacetDto
            {
                Value = group.First(),
                Count = group.Count()
            })
            .OrderByDescending(facet => facet.Count)
            .ThenBy(facet => facet.Value)
            .ToList();
    }

    private static List<ExerciseCollectionDto> BuildFeaturedCollections(
        IReadOnlyList<ExerciseListItem> orderedExercises,
        IReadOnlySet<int> favoriteIds,
        string? search)
    {
        var collections = new List<ExerciseCollectionDto>();

        if (orderedExercises.Count == 0)
        {
            return collections;
        }

        var favoriteExercises = orderedExercises
            .Where(exercise => favoriteIds.Contains(exercise.Id))
            .Take(4)
            .ToList();

        if (favoriteExercises.Count > 0)
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = "saved-favorites",
                Title = "Saved favorites",
                Description = "Your personal shortlist for faster planning and repeat sessions.",
                Exercises = favoriteExercises
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = "best-matches",
                Title = "Best matches",
                Description = "The strongest results for your current search.",
                Exercises = orderedExercises
                    .Take(4)
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        var imageRichExercises = orderedExercises
            .Where(exercise => exercise.IsEnglish && HasImage(exercise))
            .Take(4)
            .ToList();

        if (imageRichExercises.Count >= 3)
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = "visual-library",
                Title = "Visual picks",
                Description = "Image-rich English exercises that make planning and browsing faster.",
                Exercises = imageRichExercises
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        var bodyPartGroup = orderedExercises
            .Where(exercise => !string.IsNullOrWhiteSpace(exercise.BodyPart))
            .GroupBy(exercise => exercise.BodyPart!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault(group => group.Count() >= 3);

        if (bodyPartGroup != null)
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = $"body-part-{Normalize(bodyPartGroup.Key)}",
                Title = $"{bodyPartGroup.Key} focus",
                Description = "A concentrated cluster you can rotate through when planning sessions.",
                Exercises = bodyPartGroup
                    .OrderByDescending(exercise => exercise.IsEnglish)
                    .ThenByDescending(HasImage)
                    .ThenBy(exercise => exercise.Name)
                    .Take(4)
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        var equipmentGroup = orderedExercises
            .Where(exercise => !string.IsNullOrWhiteSpace(exercise.Equipment))
            .GroupBy(exercise => exercise.Equipment!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault(group => group.Count() >= 3);

        if (equipmentGroup != null)
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = $"equipment-{Normalize(equipmentGroup.Key)}",
                Title = $"{equipmentGroup.Key} options",
                Description = "Useful when you want to stay inside the same equipment setup.",
                Exercises = equipmentGroup
                    .OrderByDescending(exercise => exercise.IsEnglish)
                    .ThenByDescending(HasImage)
                    .ThenBy(exercise => exercise.Name)
                    .Take(4)
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        if (collections.Count == 0)
        {
            collections.Add(new ExerciseCollectionDto
            {
                Key = "ready-to-explore",
                Title = "Ready to explore",
                Description = "A quick starting set pulled from the current library view.",
                Exercises = orderedExercises
                    .OrderByDescending(exercise => exercise.IsEnglish)
                    .ThenByDescending(HasImage)
                    .ThenBy(exercise => exercise.Name)
                    .Take(4)
                    .Select(exercise => MapToDto(exercise, favoriteIds))
                    .ToList()
            });
        }

        return collections
            .GroupBy(collection => collection.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(3)
            .ToList();
    }

    private async Task<HashSet<int>> GetFavoriteExerciseIdsAsync(
        int profileId,
        IEnumerable<int> exerciseIds,
        CancellationToken cancellationToken)
    {
        var distinctExerciseIds = exerciseIds
            .Distinct()
            .ToList();

        if (distinctExerciseIds.Count == 0)
        {
            return [];
        }

        return await _context.FavoriteExercises
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserProfileId == profileId &&
                distinctExerciseIds.Contains(favorite.ExerciseId))
            .Select(favorite => favorite.ExerciseId)
            .ToHashSetAsync(cancellationToken);
    }

    private static ExerciseDto MapToDto(ExerciseListItem exercise, IReadOnlySet<int> favoriteIds)
    {
        return new ExerciseDto
        {
            Id = exercise.Id,
            ExternalId = exercise.ExternalId,
            Name = exercise.Name,
            Description = exercise.Description,
            Category = exercise.Category,
            BodyPart = exercise.BodyPart,
            Equipment = exercise.Equipment,
            ImageUrl = exercise.ImageUrl,
            VideoUrl = exercise.VideoUrl,
            IsEnglish = exercise.IsEnglish,
            IsFavorite = favoriteIds.Contains(exercise.Id)
        };
    }

    private static string[] SplitSearchTerms(string? search)
    {
        return string.IsNullOrWhiteSpace(search)
            ? []
            : search
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static SearchOrderingContext CreateOrderingContext(string? search)
    {
        return new SearchOrderingContext(
            Normalize(search),
            SplitSearchTerms(search)
                .Select(Normalize)
                .ToArray());
    }

    private static bool HasImage(ExerciseListItem exercise)
    {
        return !string.IsNullOrWhiteSpace(exercise.ImageUrl);
    }

    private static bool Matches(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
               !string.IsNullOrWhiteSpace(right) &&
               string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeFilterValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private sealed class ExerciseListItem
    {
        public int Id { get; init; }
        public string ExternalId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Category { get; init; }
        public string? BodyPart { get; init; }
        public string? Equipment { get; init; }
        public string? ImageUrl { get; init; }
        public string? VideoUrl { get; init; }
        public bool IsEnglish { get; init; }
    }

    private sealed record SearchOrderingContext(string NormalizedSearch, string[] Terms);
}   