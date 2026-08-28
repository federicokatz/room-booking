using RoomBooking.Application.Abstractions.Persistence;

namespace RoomBooking.Application.Rooms;

public sealed class ListRoomsUseCase(IRoomRepository roomRepository)
{
    public async Task<IReadOnlyList<RoomResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await roomRepository.ListAsync(cancellationToken);

        return rooms
            .OrderBy(room => room.Code.Value, StringComparer.Ordinal)
            .Select(RoomResponse.From)
            .ToArray();
    }
}
