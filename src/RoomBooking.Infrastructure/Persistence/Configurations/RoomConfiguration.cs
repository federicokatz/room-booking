using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomBooking.Domain.Rooms;
using RoomBooking.Infrastructure.Persistence.Seed;

namespace RoomBooking.Infrastructure.Persistence.Configurations;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(room => room.Id);

        builder.Property(room => room.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(room => room.Code)
            .HasColumnName("code")
            .HasMaxLength(1)
            .HasConversion(
                code => code.Value,
                value => RoomCode.Create(value).Value)
            .IsRequired();
        builder.Property(room => room.Capacity).HasColumnName("capacity").IsRequired();

        builder.HasIndex(room => room.Code).IsUnique();
        builder.HasData(DefaultRooms.All);
    }
}
