namespace FirstTryApi.Exceptions;

public class GameException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public GameException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
