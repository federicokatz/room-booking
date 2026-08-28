using Microsoft.Extensions.DependencyInjection;
using RoomBooking.Application.Agent;
using RoomBooking.Application.Agent.Tools;
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
        services.AddSingleton<ChatSessionStore>();
        services.AddScoped<IAgentTool, CreateBookingTool>();
        services.AddScoped<IAgentTool, ListAvailableRoomsTool>();
        services.AddScoped<IAgentTool, GetRoomScheduleTool>();
        services.AddScoped<IAgentTool, ListMyBookingsTool>();
        services.AddScoped<IAgentTool, CancelBookingTool>();
        services.AddScoped<ChatAgentService>();

        return services;
    }
}
