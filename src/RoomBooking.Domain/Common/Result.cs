namespace RoomBooking.Domain.Common;

public static class Result
{
    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new Result<TValue>(value, null);
    }

    public static Result<TValue> Failure<TValue>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TValue>(default, error);
    }
}

public sealed class Result<TValue>
{
    private readonly TValue? value;

    internal Result(TValue? value, DomainError? error)
    {
        this.value = error is null ? value : default;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public DomainError? Error { get; }

    public TValue Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result does not contain a value.");
}
