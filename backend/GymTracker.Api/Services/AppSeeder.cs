using GymTracker.Api.Data;
using GymTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Services;

public class AppSeeder
{
    private readonly AppDbContext _context;
    private readonly ExerciseImportService _exerciseImportService;
    private readonly ILogger<AppSeeder> _logger;
    private int? _defaultProfileId;

    public AppSeeder(
        AppDbContext context,
        ExerciseImportService exerciseImportService,
        ILogger<AppSeeder> logger)
    {
        _context = context;
        _exerciseImportService = exerciseImportService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await EnsureDefaultUserProfileAsync();
        await SeedExercisesAsync();
        await SeedStarterPlansAsync();
    }

    private async Task EnsureDefaultUserProfileAsync()
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(item => item.Key == UserProfileDefaults.DefaultProfileKey);

        if (profile == null)
        {
            profile = new UserProfile
            {
                Key = UserProfileDefaults.DefaultProfileKey,
                DisplayName = UserProfileDefaults.DefaultProfileName,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created default user profile for local development.");
        }

        _defaultProfileId = profile.Id;
    }

    private async Task SeedExercisesAsync()
    {
        var hasExercises = await _context.Exercises.AnyAsync();
        if (hasExercises)
        {
            _logger.LogInformation("Exercises already exist. Skipping exercise seed.");
            return;
        }

        _logger.LogInformation("No exercises found. Importing real exercises from wger...");

        try
        {
            var imported = await _exerciseImportService.SyncExercisesFromWgerAsync();
            _logger.LogInformation("Exercise import finished. Imported {ImportedCount} exercises.", imported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import exercises from wger.");
        }
    }

    private async Task SeedStarterPlansAsync()
    {
        var defaultProfileId = GetDefaultProfileId();

        var hasPlans = await _context.WorkoutPlans.AnyAsync(plan => plan.UserProfileId == defaultProfileId);
        if (hasPlans)
        {
            _logger.LogInformation("Workout plans already exist. Skipping starter plan seed.");
            return;
        }

        var exercises = await _context.Exercises
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();

        if (exercises.Count == 0)
        {
            _logger.LogWarning("No exercises available, so starter plans could not be created.");
            return;
        }

        var starterPlans = BuildStarterPlans(exercises);

        if (starterPlans.Count == 0)
        {
            _logger.LogWarning("No starter plans could be built from available exercises.");
            return;
        }

        _context.WorkoutPlans.AddRange(starterPlans);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {PlanCount} starter workout plans.", starterPlans.Count);
    }

    private List<WorkoutPlan> BuildStarterPlans(List<Exercise> exercises)
    {
        var defaultProfileId = GetDefaultProfileId();
        var plans = new List<WorkoutPlan>();

        AddPlanIfValid(plans, CreateBeginnerFullBodyA(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreateBeginnerFullBodyB(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreatePushDay(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreatePullDay(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreateLegDay(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreateUpperBody(exercises, defaultProfileId));
        AddPlanIfValid(plans, CreateLowerBody(exercises, defaultProfileId));

        return plans;
    }

    private static void AddPlanIfValid(List<WorkoutPlan> plans, WorkoutPlan? plan)
    {
        if (plan != null && plan.Exercises.Count >= 4)
        {
            plans.Add(plan);
        }
    }

    private WorkoutPlan? CreateBeginnerFullBodyA(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Beginner Full Body A",
            "A simple full-body session focused on compound movements and easy progression.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "squat", "bodyweight squat", "barbell squat" }, 3, 10, "Controlled tempo."),
                PlanItem(new[] { "bench press", "push up", "chest press" }, 3, 10, "Stay stable through the upper back."),
                PlanItem(new[] { "lat pulldown", "pull up", "chin up" }, 3, 10, "Pull elbows down and back."),
                PlanItem(new[] { "romanian deadlift", "deadlift" }, 3, 10, "Keep the hinge clean."),
                PlanItem(new[] { "plank", "crunch" }, 3, 30, "Brace and breathe.")
            });
    }

    private WorkoutPlan? CreateBeginnerFullBodyB(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Beginner Full Body B",
            "Second full-body day with slightly different movement patterns and angles.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "leg press", "lunge", "split squat" }, 3, 12, "Drive through full foot."),
                PlanItem(new[] { "incline bench press", "incline dumbbell press", "shoulder press" }, 3, 10, "Press under control."),
                PlanItem(new[] { "seated row", "cable row", "row" }, 3, 10, "Squeeze shoulder blades."),
                PlanItem(new[] { "glute bridge", "hip thrust" }, 3, 12, "Pause at the top."),
                PlanItem(new[] { "lateral raise", "side raise" }, 3, 15, "Light weight, strict form.")
            });
    }

    private WorkoutPlan? CreatePushDay(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Push Day",
            "Chest, shoulders, and triceps focused hypertrophy session.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "bench press", "chest press" }, 4, 8, "Main press movement."),
                PlanItem(new[] { "incline dumbbell press", "incline bench press" }, 3, 10, "Upper chest focus."),
                PlanItem(new[] { "shoulder press", "overhead press" }, 3, 10, "Stay stacked."),
                PlanItem(new[] { "lateral raise", "side raise" }, 3, 15, "Smooth reps."),
                PlanItem(new[] { "triceps pushdown", "tricep pushdown", "triceps extension" }, 3, 12, "Full lockout.")
            });
    }

    private WorkoutPlan? CreatePullDay(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Pull Day",
            "Back and biceps focused session built around vertical and horizontal pulling.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "lat pulldown", "pull up", "chin up" }, 4, 8, "Lead with elbows."),
                PlanItem(new[] { "seated row", "cable row", "row" }, 3, 10, "Squeeze at the end."),
                PlanItem(new[] { "face pull" }, 3, 15, "Great for posture."),
                PlanItem(new[] { "rear delt fly", "reverse fly" }, 3, 15, "Light and controlled."),
                PlanItem(new[] { "biceps curl", "dumbbell curl", "barbell curl" }, 3, 12, "No swinging.")
            });
    }

    private WorkoutPlan? CreateLegDay(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Leg Day",
            "Lower body session focused on quads, glutes, and hamstrings.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "squat", "barbell squat", "hack squat" }, 4, 8, "Main lower-body lift."),
                PlanItem(new[] { "leg press" }, 3, 12, "Controlled descent."),
                PlanItem(new[] { "romanian deadlift", "deadlift" }, 3, 10, "Stretch hamstrings."),
                PlanItem(new[] { "leg curl", "hamstring curl" }, 3, 12, "Pause at squeeze."),
                PlanItem(new[] { "calf raise" }, 4, 15, "Slow reps, full range.")
            });
    }

    private WorkoutPlan? CreateUpperBody(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Upper Body",
            "Balanced upper-body session for days when you want one combined workout.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "bench press", "chest press" }, 3, 8, "Primary push."),
                PlanItem(new[] { "lat pulldown", "pull up" }, 3, 10, "Primary pull."),
                PlanItem(new[] { "shoulder press", "overhead press" }, 3, 10, "Shoulder strength."),
                PlanItem(new[] { "seated row", "cable row" }, 3, 10, "Horizontal pull."),
                PlanItem(new[] { "biceps curl", "dumbbell curl" }, 2, 12, "Arm finisher."),
                PlanItem(new[] { "triceps pushdown", "triceps extension" }, 2, 12, "Arm finisher.")
            });
    }

    private WorkoutPlan? CreateLowerBody(List<Exercise> exercises, int userProfileId)
    {
        return CreatePlan(
            "Lower Body",
            "Balanced lower-body training day with strength and volume work.",
            exercises,
            userProfileId,
            new[]
            {
                PlanItem(new[] { "leg press", "squat" }, 4, 10, "Heavy first movement."),
                PlanItem(new[] { "walking lunge", "lunge", "split squat" }, 3, 12, "Single-leg stability."),
                PlanItem(new[] { "romanian deadlift", "stiff leg deadlift" }, 3, 10, "Posterior chain."),
                PlanItem(new[] { "leg extension" }, 3, 15, "Quad burn."),
                PlanItem(new[] { "leg curl", "hamstring curl" }, 3, 15, "Hamstring burn.")
            });
    }

    private WorkoutPlan? CreatePlan(
        string name,
        string description,
        List<Exercise> exercises,
        int userProfileId,
        PlanExerciseSeed[] items)
    {
        var planExercises = new List<WorkoutPlanExercise>();
        var usedExerciseIds = new HashSet<int>();
        var order = 1;

        foreach (var item in items)
        {
            var exercise = FindBestExercise(exercises, item.Candidates, usedExerciseIds);
            if (exercise == null)
                continue;

            usedExerciseIds.Add(exercise.Id);

            planExercises.Add(new WorkoutPlanExercise
            {
                ExerciseId = exercise.Id,
                OrderIndex = order++,
                TargetSets = item.Sets,
                TargetReps = item.Reps,
                Notes = item.Notes
            });
        }

        if (planExercises.Count < 4)
            return null;

        return new WorkoutPlan
        {
            UserProfileId = userProfileId,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Exercises = planExercises
        };
    }

    private int GetDefaultProfileId()
    {
        return _defaultProfileId ?? throw new InvalidOperationException("Default profile has not been initialized.");
    }

    private static Exercise? FindBestExercise(
        List<Exercise> exercises,
        string[] candidates,
        HashSet<int> usedExerciseIds)
    {
        foreach (var candidate in candidates)
        {
            var normalizedCandidate = Normalize(candidate);

            var exact = exercises.FirstOrDefault(e =>
                !usedExerciseIds.Contains(e.Id) &&
                Normalize(e.Name) == normalizedCandidate);

            if (exact != null)
                return exact;

            var contains = exercises.FirstOrDefault(e =>
                !usedExerciseIds.Contains(e.Id) &&
                Normalize(e.Name).Contains(normalizedCandidate));

            if (contains != null)
                return contains;

            var reverseContains = exercises.FirstOrDefault(e =>
                !usedExerciseIds.Contains(e.Id) &&
                normalizedCandidate.Contains(Normalize(e.Name)));

            if (reverseContains != null)
                return reverseContains;
        }

        return null;
    }

    private static string Normalize(string value)
    {
        return new string(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static PlanExerciseSeed PlanItem(string[] candidates, int sets, int reps, string notes)
    {
        return new PlanExerciseSeed(candidates, sets, reps, notes);
    }

    private sealed record PlanExerciseSeed(string[] Candidates, int Sets, int Reps, string Notes);
}
