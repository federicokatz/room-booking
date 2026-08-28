using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Infrastructure.Persistence;

public sealed class RoomBookingDbContext(DbContextOptions<RoomBookingDbContext> options)
    : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoomBookingDbContext).Assembly);
    }
}
