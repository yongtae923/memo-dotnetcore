namespace AblyAPI.Models.DTO;

public class StatusResponse
{
    public StatusType Status { get; set; }
    public object? Body { get; set; }

    public StatusResponse(StatusType statusType, object? body = null)
    {
        Status = statusType;
        Body = body;
    }
}

public enum StatusType
{
    Success,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    RequestTimeout,
    Conflict
}