namespace RoomBooking.Application.Agent.Tools;

public static class AgentToolNames
{
    public const string CreateBooking = "create_booking";
    public const string ListAvailableRooms = "list_available_rooms";
    public const string GetRoomSchedule = "get_room_schedule";
    public const string ListMyBookings = "list_my_bookings";
    public const string CancelBooking = "cancel_booking";
}

public static class AgentEffects
{
    public const string BookingCreated = "booking_created";
    public const string BookingCancelled = "booking_cancelled";
}
