namespace BusinessService.Application.Interfaces;


public interface IUserServiceClient
{
    Task<bool> IsSupportUserAsync(Guid userId);
}