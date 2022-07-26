using AblyAPI.Models.Responses;

namespace AblyAPI.Services;

public class UserService
{
    private readonly DatabaseContext _database;

    public UserService(DatabaseContext database)
    {
        _database = database;
    }
    
}