using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomBooking.Domain.Bookings;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(booking => booking.RoomId).HasColumnName("room_id").IsRequired();
        builder.Property(booking => booking.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(booking => booking.Title)
            .HasColumnName("title")
            .HasMaxLength(Booking.MaxTitleLength)
            .IsRequired();
        builder.Property(booking => booking.Attendees).HasColumnName("attendees").IsRequired();
        builder.Property(booking => booking.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();
        builder.Property(booking => booking.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.OwnsOne(booking => booking.Period, period =>
        {
            period.Property(value => value.StartUtc)
                .HasColumnName("start_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            period.Property(value => value.EndUtc)
                .HasColumnName("end_utc")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });
        builder.Navigation(booking => booking.Period).IsRequired();

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(booking => new { booking.RoomId, booking.Status });
        builder.HasIndex(booking => new { booking.OwnerId, booking.Status });
    }
}
