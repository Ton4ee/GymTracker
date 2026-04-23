using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GymTracker.Api.Data;
using GymTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Services;

public class ExerciseImportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public ExerciseImportService(
        IHttpClientFactory httpClientFactory,
        AppDbContext context,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _configuration = configuration;
    }

    public async Task<int> SyncExercisesFromWgerAsync()
    {
        var client = _httpClientFactory.CreateClient();

        var baseUrl = (_configuration["Wger:BaseUrl"] ?? "https://wger.de/api/v2").TrimEnd('/');

        var candidateUrls = new[]
        {
            $"{baseUrl}/exercisebaseinfo/?limit=100",
            $"{baseUrl}/exerciseinfo/?limit=100"
        };

        string? url = null;

        foreach (var candidate in candidateUrls)
        {
            Console.WriteLine($"[ExerciseImport] Testing endpoint: {candidate}");

            using var testResponse = await client.GetAsync(candidate);

            Console.WriteLine(
                $"[ExerciseImport] Status for {candidate}: {(int)testResponse.StatusCode} {testResponse.StatusCode}");

            if (testResponse.IsSuccessStatusCode)
            {
                url = candidate;
                break;
            }

            if (testResponse.StatusCode != HttpStatusCode.NotFound)
            {
                testResponse.EnsureSuccessStatusCode();
            }
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("No working wger exercise endpoint was found.");
        }

        Console.WriteLine($"[ExerciseImport] Using endpoint: {url}");

        var importedCount = 0;
        var englishExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine($"[ExerciseImport] Requesting: {url}");

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            var root = document.RootElement;

            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                Console.WriteLine("[ExerciseImport] No results array found.");
                break;
            }

            var pageCount = 0;

            foreach (var item in results.EnumerateArray())
            {
                pageCount++;

                var externalId = GetString(item, "id");
                if (string.IsNullOrWhiteSpace(externalId))
                    continue;

                var englishTranslation = FindEnglishTranslation(item);
                if (!englishTranslation.HasValue)
                    continue;

                var name = CleanText(ExtractEnglishName(englishTranslation.Value));
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                englishExternalIds.Add(externalId);

                var description = CleanText(ExtractEnglishDescription(englishTranslation.Value));
                var category = CleanText(ExtractCategory(item));
                var bodyPart = CleanText(ExtractMuscles(item));
                var equipment = CleanText(ExtractEquipment(item));
                var imageUrl = ExtractImageUrl(item);
                var videoUrl = ExtractVideoUrl(item);

                var existing = await _context.Exercises
                    .FirstOrDefaultAsync(e => e.ExternalId == externalId && e.Source == "wger");

                if (existing == null)
                {
                    _context.Exercises.Add(new Exercise
                    {
                        ExternalId = externalId,
                        Name = name,
                        Description = description,
                        Category = category,
                        BodyPart = bodyPart,
                        Equipment = equipment,
                        ImageUrl = imageUrl,
                        VideoUrl = videoUrl,
                        IsEnglish = true,
                        Source = "wger",
                        LastSyncedAt = DateTime.UtcNow
                    });

                    importedCount++;
                }
                else
                {
                    existing.Name = name;
                    existing.Description = description;
                    existing.Category = category;
                    existing.BodyPart = bodyPart;
                    existing.Equipment = equipment;
                    existing.ImageUrl = imageUrl;
                    existing.VideoUrl = videoUrl;
                    existing.IsEnglish = true;
                    existing.LastSyncedAt = DateTime.UtcNow;
                }
            }

            Console.WriteLine($"[ExerciseImport] Parsed {pageCount} records on this page.");

            await _context.SaveChangesAsync();

            url = GetNextUrl(root);
            Console.WriteLine($"[ExerciseImport] Next URL: {url ?? "(none)"}");
        }

        var staleOrNonEnglishExercises = await _context.Exercises
            .Where(exercise =>
                exercise.Source == "wger" &&
                !englishExternalIds.Contains(exercise.ExternalId))
            .ToListAsync();

        foreach (var exercise in staleOrNonEnglishExercises)
        {
            exercise.IsEnglish = false;
            exercise.LastSyncedAt = DateTime.UtcNow;
        }

        if (staleOrNonEnglishExercises.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        Console.WriteLine($"[ExerciseImport] Finished. Imported {importedCount} new exercises.");
        return importedCount;
    }

    private static string? GetNextUrl(JsonElement root) =>
        root.TryGetProperty("next", out var next) && next.ValueKind == JsonValueKind.String
            ? next.GetString()
            : null;

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return string.Empty;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            _ => string.Empty
        };
    }

    private static string? ExtractEnglishName(JsonElement translation)
    {
        if (translation.TryGetProperty("name", out var englishNameElement) &&
            englishNameElement.ValueKind == JsonValueKind.String)
        {
            var value = englishNameElement.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? ExtractEnglishDescription(JsonElement translation)
    {
        if (translation.TryGetProperty("description", out var englishDescriptionElement) &&
            englishDescriptionElement.ValueKind == JsonValueKind.String)
        {
            var value = englishDescriptionElement.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static JsonElement? FindEnglishTranslation(JsonElement item)
    {
        if (!item.TryGetProperty("translations", out var translations) || translations.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var translation in translations.EnumerateArray())
        {
            if (IsEnglishTranslation(translation))
                return translation;
        }

        return null;
    }

    private static bool IsEnglishTranslation(JsonElement translation)
    {
        if (translation.TryGetProperty("language", out var language))
        {
            if (language.ValueKind == JsonValueKind.Object)
            {
                var candidates = new List<string>();

                if (language.TryGetProperty("short_name", out var shortName) && shortName.ValueKind == JsonValueKind.String)
                    candidates.Add(shortName.GetString() ?? string.Empty);

                if (language.TryGetProperty("full_name", out var fullName) && fullName.ValueKind == JsonValueKind.String)
                    candidates.Add(fullName.GetString() ?? string.Empty);

                if (language.TryGetProperty("name", out var languageName) && languageName.ValueKind == JsonValueKind.String)
                    candidates.Add(languageName.GetString() ?? string.Empty);

                if (candidates.Any(value =>
                        value.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("english", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            else if (language.ValueKind == JsonValueKind.String)
            {
                var value = language.GetString() ?? string.Empty;
                if (value.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("english", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        if (translation.TryGetProperty("language_short", out var languageShort) &&
            languageShort.ValueKind == JsonValueKind.String &&
            string.Equals(languageShort.GetString(), "en", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string? ExtractCategory(JsonElement item)
    {
        if (item.TryGetProperty("category", out var category))
        {
            if (category.ValueKind == JsonValueKind.Object &&
                category.TryGetProperty("name", out var categoryName) &&
                categoryName.ValueKind == JsonValueKind.String)
                return categoryName.GetString();

            if (category.ValueKind == JsonValueKind.String)
                return category.GetString();
        }

        return null;
    }

    private static string? ExtractMuscles(JsonElement item)
    {
        if (item.TryGetProperty("muscles", out var muscles) && muscles.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();

            foreach (var muscle in muscles.EnumerateArray())
            {
                if (muscle.ValueKind == JsonValueKind.Object)
                {
                    if (muscle.TryGetProperty("name_en", out var nameEn) && nameEn.ValueKind == JsonValueKind.String)
                    {
                        var value = nameEn.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value!);
                    }
                    else if (muscle.TryGetProperty("name", out var muscleName) && muscleName.ValueKind == JsonValueKind.String)
                    {
                        var value = muscleName.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value!);
                    }
                }
            }

            return values.Count > 0 ? string.Join(", ", values.Distinct()) : null;
        }

        return null;
    }

    private static string? ExtractEquipment(JsonElement item)
    {
        if (item.TryGetProperty("equipment", out var equipment) && equipment.ValueKind == JsonValueKind.Array)
        {
            var values = new List<string>();

            foreach (var eq in equipment.EnumerateArray())
            {
                if (eq.ValueKind == JsonValueKind.Object &&
                    eq.TryGetProperty("name", out var equipmentName) &&
                    equipmentName.ValueKind == JsonValueKind.String)
                {
                    var value = equipmentName.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(value!);
                }
            }

            return values.Count > 0 ? string.Join(", ", values.Distinct()) : null;
        }

        return null;
    }

    private static string? ExtractImageUrl(JsonElement item)
    {
        if (item.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in images.EnumerateArray())
            {
                if (image.TryGetProperty("image", out var imageUrl) && imageUrl.ValueKind == JsonValueKind.String)
                {
                    var value = imageUrl.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
        }

        return null;
    }

    private static string? ExtractVideoUrl(JsonElement item)
    {
        if (item.TryGetProperty("videos", out var videos) && videos.ValueKind == JsonValueKind.Array)
        {
            foreach (var video in videos.EnumerateArray())
            {
                if (video.TryGetProperty("video", out var videoUrl) && videoUrl.ValueKind == JsonValueKind.String)
                {
                    var value = videoUrl.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
        }

        return null;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = Regex.Replace(decoded, "<.*?>", " ");
        var normalizedWhitespace = Regex.Replace(withoutTags, @"\s+", " ").Trim();

        return string.IsNullOrWhiteSpace(normalizedWhitespace)
            ? null
            : normalizedWhitespace;
    }
}