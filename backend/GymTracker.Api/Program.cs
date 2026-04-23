using GymTracker.Api.Data;
using GymTracker.Api.Interfaces;
using GymTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ExerciseImportService>();
builder.Services.AddScoped<ExerciseQueryService>();
builder.Services.AddScoped<AppSeeder>();
builder.Services.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupSeeder");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    var exerciseCountBefore = await db.Exercises.CountAsync();
    var planCountBefore = await db.WorkoutPlans.CountAsync();

    logger.LogInformation("Before seeding: Exercises={ExerciseCount}, Plans={PlanCount}", exerciseCountBefore, planCountBefore);

    var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();
    await seeder.SeedAsync();

    var exerciseCountAfter = await db.Exercises.CountAsync();
    var planCountAfter = await db.WorkoutPlans.CountAsync();

    logger.LogInformation("After seeding: Exercises={ExerciseCount}, Plans={PlanCount}", exerciseCountAfter, planCountAfter);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();
app.Run();
