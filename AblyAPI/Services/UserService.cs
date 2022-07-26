using AblyAPI.Models.Responses;

namespace AblyAPI.Services;

public interface IUserService
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<StatusResponse> GetUserInformation();
}

public class UserService : IUserService
{
    private readonly DatabaseContext _database;

    public UserService(DatabaseContext database)
    {
        _database = database;
    }

    public Task<StatusResponse> GetUserInformation()
    {
        throw new NotImplementedException();
    }
}