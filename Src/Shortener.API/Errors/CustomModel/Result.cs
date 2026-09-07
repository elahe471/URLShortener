namespace Shortener.API.Errors.CustomModel;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }

    private Result(
        bool isSuccess,
        T? value,
        string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value)
        => new(true, value, null);

    public static Result<T> Failure(string errorCode)
        => new(false, default, errorCode);
}
