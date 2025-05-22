namespace auth.Models
{
public interface IApiResponse
{
    bool Success { get; set; }
    string Message { get; set; }
}

public class ApiResponse : IApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class ApiResponse<T> : ApiResponse, IApiResponse
{
    public T Data { get; set; }
}

}