using GymTracker.Api.Data;
using GymTracker.Api.Entities;
using GymTracker.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Api.Services;

public class CurrentUserProfileService : ICurrentUserProfileService
{
    private const string CachedProfileItemKey = "__current_user_profile";

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProfileService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserProfile> GetCurrentProfileAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.Items.TryGetValue(CachedProfileItemKey, out var cachedProfile) == true &&
            cachedProfile is UserProfile cachedUserProfile)
        {
            return cachedUserProfile;
        }

        var profileKey = ResolveProfileKey();

        UserProfile? profile = await _context.UserProfiles
            .FirstOrDefaultAsync(item => item.Key == profileKey, cancellationToken);

        if (profile == null)
        {
            profile = new UserProfile
            {
                Key = profileKey,
                DisplayName = profileKey == UserProfileDefaults.DefaultProfileKey
                    ? UserProfileDefaults.DefaultProfileName
                    : profileKey
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (httpContext != null)
        {
            httpContext.Items[CachedProfileItemKey] = profile;
        }

        return profile;
    }

    public async Task<int> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        return profile.Id;
    }

    private string ResolveProfileKey()
    {
        var rawKey = _httpContextAccessor.HttpContext?.Request.Headers[UserProfileDefaults.HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return UserProfileDefaults.DefaultProfileKey;
        }

        var normalized = new string(rawKey
            .Trim()
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .Take(64)
            .ToArray());

        return string.IsNullOrWhiteSpace(normalized)
            ? UserProfileDefaults.DefaultProfileKey
            : normalized;
    }
}
