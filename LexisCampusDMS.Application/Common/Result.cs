namespace LexisCampusDMS.Application.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    protected Result(bool isSuccess, string message, string? errorCode = null, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        ErrorCode = errorCode;
        Errors = errors is not null ? errors.ToList() : [];
    }

    public static Result Success(string message = "Operación completada exitosamente.")
    {
        return new Result(true, message);
    }

    public static Result Failure(string message, string? errorCode = null, IEnumerable<string>? errors = null)
    {
        return new Result(false, message, errorCode, errors);
    }
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    protected Result(bool isSuccess, T? data, string message, string? errorCode = null, IEnumerable<string>? errors = null)
        : base(isSuccess, message, errorCode, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Operación completada exitosamente.")
    {
        return new Result<T>(true, data, message);
    }

    public new static Result<T> Failure(string message, string? errorCode = null, IEnumerable<string>? errors = null)
    {
        return new Result<T>(false, default, message, errorCode, errors);
    }
}

public class PagedResult<T> : Result<IReadOnlyList<T>>
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(
        IReadOnlyList<T> items, 
        int totalCount, 
        int pageNumber, 
        int pageSize, 
        string message = "Consulta paginada completada exitosamente.")
        : base(true, items, message)
    {
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
