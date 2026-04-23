using GymTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<WorkoutPlanExercise> WorkoutPlanExercises => Set<WorkoutPlanExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<BodyWeightLog> BodyWeightLogs => Set<BodyWeightLog>();
    public DbSet<FavoriteExercise> FavoriteExercises => Set<FavoriteExercise>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>()
            .HasIndex(profile => profile.Key)
            .IsUnique();

        modelBuilder.Entity<Exercise>()
            .HasIndex(e => e.ExternalId);

        modelBuilder.Entity<WorkoutPlan>()
            .HasOne(plan => plan.UserProfile)
            .WithMany(profile => profile.WorkoutPlans)
            .HasForeignKey(plan => plan.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutPlanExercise>()
            .HasOne(wpe => wpe.WorkoutPlan)
            .WithMany(wp => wp.Exercises)
            .HasForeignKey(wpe => wpe.WorkoutPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutPlanExercise>()
            .HasOne(wpe => wpe.Exercise)
            .WithMany(e => e.WorkoutPlanExercises)
            .HasForeignKey(wpe => wpe.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(session => session.UserProfile)
            .WithMany(profile => profile.WorkoutSessions)
            .HasForeignKey(session => session.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSessionExercise>()
            .HasOne(wse => wse.WorkoutSession)
            .WithMany(ws => ws.Exercises)
            .HasForeignKey(wse => wse.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSessionExercise>()
            .HasOne(wse => wse.Exercise)
            .WithMany(e => e.WorkoutSessionExercises)
            .HasForeignKey(wse => wse.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BodyWeightLog>()
            .HasOne(log => log.UserProfile)
            .WithMany(profile => profile.BodyWeightLogs)
            .HasForeignKey(log => log.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteExercise>()
            .HasOne(fe => fe.UserProfile)
            .WithMany(profile => profile.FavoriteExercises)
            .HasForeignKey(fe => fe.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteExercise>()
            .HasOne(fe => fe.Exercise)
            .WithMany(e => e.FavoriteExercises)
            .HasForeignKey(fe => fe.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoriteExercise>()
            .HasIndex(fe => new { fe.UserProfileId, fe.ExerciseId })
            .IsUnique();
    }
}
