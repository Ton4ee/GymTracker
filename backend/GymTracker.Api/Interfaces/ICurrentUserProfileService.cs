using GymTracker.Api.Entities;

namespace GymTracker.Api.Interfaces;

public interface ICurrentUserProfileService
{
    Task<UserProfile> GetCurrentProfileAsync(CancellationToken cancellationToken = default);
    Task<int> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default);
}
