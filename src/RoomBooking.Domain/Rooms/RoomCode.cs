using RoomBooking.Domain.Common;

namespace RoomBooking.Domain.Rooms;

public sealed record RoomCode
{
    private static readonly Dictionary<string, RoomCode> KnownCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new("A"),
            ["B"] = new("B"),
            ["C"] = new("C"),
            ["D"] = new("D"),
            ["E"] = new("E")
        };

    private RoomCode(string value)
    {
        Value = value;
    }

    public static RoomCode A => KnownCodes["A"];

    public static RoomCode B => KnownCodes["B"];

    public static RoomCode C => KnownCodes["C"];

    public static RoomCode D => KnownCodes["D"];

    public static RoomCode E => KnownCodes["E"];

    public static IReadOnlyCollection<RoomCode> All => KnownCodes.Values;

    public string Value { get; }

    public static Result<RoomCode> Create(string? value)
    {
        var normalizedValue = value?.Trim();

        return normalizedValue is not null && KnownCodes.TryGetValue(normalizedValue, out var code)
            ? Result.Success(code)
            : Result.Failure<RoomCode>(RoomErrors.InvalidCode);
    }

    public override string ToString()
    {
        return Value;
    }
}
