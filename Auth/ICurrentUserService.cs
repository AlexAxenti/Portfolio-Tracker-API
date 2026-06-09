namespace PortfolioTracker.Api.Auth;

public interface ICurrentUserService
{
    Guid UserId { get; }
}
