using GymTracker.Api.Data;
using GymTracker.Api.Interfaces;
using GymTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = ResolveConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ExerciseImportService>();
builder.Services.AddScoped<ExerciseQueryService>();
builder.Services.AddScoped<AppSeeder>();
builder.Services.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();

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

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapFallbackToFile("index.html");
app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var configured = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured;
    }

    var databaseUrl = configuration["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(databaseUrl) && Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
    {
        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(credentials.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Require
        }.ConnectionString;
    }

    throw new InvalidOperationException(
        "A PostgreSQL connection is required. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");
}
