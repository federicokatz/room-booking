using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Bookings.CancelBooking;
using RoomBooking.Application.Bookings.CreateBooking;
using RoomBooking.Application.Bookings.GetRoomSchedule;
using RoomBooking.Application.Bookings.ListAvailableRooms;
using RoomBooking.Application.Bookings.ListMyBookings;
using RoomBooking.Application.Rooms;

namespace RoomBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateBookingUseCase>();
        services.AddScoped<ListAvailableRoomsUseCase>();
        services.AddScoped<GetRoomScheduleUseCase>();
        services.AddScoped<ListMyBookingsUseCase>();
        services.AddScoped<CancelBookingUseCase>();
        services.AddScoped<ListRoomsUseCase>();

        return services;
    }
}
