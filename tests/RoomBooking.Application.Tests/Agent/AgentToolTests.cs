using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoomBooking.Application.Agent.Tools;
using RoomBooking.Application.Bookings.CancelBooking;
using RoomBooking.Application.Bookings.CreateBooking;
using RoomBooking.Application.Bookings.GetRoomSchedule;
using RoomBooking.Application.Bookings.ListAvailableRooms;
using RoomBooking.Application.Bookings.ListMyBookings;
using RoomBooking.Application.Tests.Fakes;
using RoomBooking.Domain.Rooms;

namespace RoomBooking.Application.Tests.Agent;

[TestClass]
public class AgentToolTests
{
    [TestMethod]
    public async Task CreateBookingDelegatesToUseCaseAndUsesCurrentUser()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        var tool = new CreateBookingTool(new CreateBookingUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings));

        var result = await tool.ExecuteAsync(
            "{\"roomCode\":\"A\",\"startUtc\":\"2026-09-01T10:00:00+00:00\",\"endUtc\":\"2026-09-01T11:00:00+00:00\",\"title\":\"Interview\",\"attendees\":3}");

        ReadSuccess(result).Should().BeTrue();
        result.Effect.Should().Be(AgentEffects.BookingCreated);
        bookings.Bookings.Should().ContainSingle()
            .Which.OwnerId.Should().Be("User1");
    }

    [TestMethod]
    public async Task CreateBookingRejectsModelSuppliedUserId()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        var tool = new CreateBookingTool(new CreateBookingUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings));

        var result = await tool.ExecuteAsync(
            "{\"roomCode\":\"A\",\"startUtc\":\"2026-09-01T10:00:00+00:00\",\"endUtc\":\"2026-09-01T11:00:00+00:00\",\"title\":\"Interview\",\"attendees\":3,\"userId\":\"User2\"}");

        ReadSuccess(result).Should().BeFalse();
        ReadCode(result).Should().Be("tool.invalid_arguments");
        bookings.Bookings.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CreateBookingRejectsMalformedArguments()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var bookings = new FakeBookingRepository();
        var tool = new CreateBookingTool(new CreateBookingUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings));

        var result = await tool.ExecuteAsync("{not-json}");

        ReadSuccess(result).Should().BeFalse();
        ReadCode(result).Should().Be("tool.invalid_arguments");
        bookings.Bookings.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ListAvailableRoomsDelegatesToUseCase()
    {
        var roomA = TestData.CreateRoom(RoomCode.A, 4);
        var roomB = TestData.CreateRoom(RoomCode.B, 6);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            roomA,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11)));
        var tool = new ListAvailableRoomsTool(new ListAvailableRoomsUseCase(
            new FakeRoomRepository([roomA, roomB]),
            bookings));

        var result = await tool.ExecuteAsync(
            "{\"startUtc\":\"2026-09-01T10:00:00+00:00\",\"endUtc\":\"2026-09-01T11:00:00+00:00\",\"attendees\":4}");

        var data = ReadData(result);
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("code").GetString().Should().Be("B");
    }

    [TestMethod]
    public async Task GetRoomScheduleDelegatesToUseCase()
    {
        var room = TestData.CreateRoom(RoomCode.B, 6);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10, 30),
            TestData.Utc(11)));
        var tool = new GetRoomScheduleTool(new GetRoomScheduleUseCase(
            new FakeRoomRepository([room]),
            bookings));

        var result = await tool.ExecuteAsync(
            "{\"roomCode\":\"B\",\"startUtc\":\"2026-09-01T10:00:00+00:00\",\"endUtc\":\"2026-09-01T11:30:00+00:00\"}");

        var slots = ReadData(result).GetProperty("slots");
        slots.EnumerateArray()
            .Select(slot => slot.GetProperty("isOccupied").GetBoolean())
            .Should().Equal(false, true, false);
    }

    [TestMethod]
    public async Task ListMyBookingsUsesAuthenticatedUser()
    {
        var room = TestData.CreateRoom(RoomCode.C, 8);
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11),
            "Mine"));
        bookings.Bookings.Add(TestData.CreateBooking(
            room,
            "User2",
            TestData.Utc(11),
            TestData.Utc(12),
            "Someone else's"));
        var tool = new ListMyBookingsTool(new ListMyBookingsUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings,
            new StubTimeProvider(TestData.Utc(9))));

        var result = await tool.ExecuteAsync("{}");

        var data = ReadData(result);
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("title").GetString().Should().Be("Mine");
    }

    [TestMethod]
    public async Task CancelBookingDelegatesOwnershipCheckToUseCase()
    {
        var room = TestData.CreateRoom(RoomCode.D, 10);
        var booking = TestData.CreateBooking(
            room,
            "User2",
            TestData.Utc(10),
            TestData.Utc(11));
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(booking);
        var tool = new CancelBookingTool(new CancelBookingUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings,
            new StubTimeProvider(TestData.Utc(9))));

        var result = await tool.ExecuteAsync($"{{\"bookingId\":\"{booking.Id}\"}}");

        ReadSuccess(result).Should().BeFalse();
        ReadCode(result).Should().Be("booking.not_owner");
        result.Effect.Should().BeNull();
    }

    [TestMethod]
    public async Task CancelBookingReturnsEffectOnlyAfterSuccessfulCancellation()
    {
        var room = TestData.CreateRoom(RoomCode.D, 10);
        var booking = TestData.CreateBooking(
            room,
            "User1",
            TestData.Utc(10),
            TestData.Utc(11));
        var bookings = new FakeBookingRepository();
        bookings.Bookings.Add(booking);
        var tool = new CancelBookingTool(new CancelBookingUseCase(
            new FakeCurrentUser("User1"),
            new FakeRoomRepository([room]),
            bookings,
            new StubTimeProvider(TestData.Utc(9))));

        var result = await tool.ExecuteAsync($"{{\"bookingId\":\"{booking.Id}\"}}");

        ReadSuccess(result).Should().BeTrue();
        result.Effect.Should().Be(AgentEffects.BookingCancelled);
        bookings.SaveChangesCount.Should().Be(1);
    }

    [TestMethod]
    public void ToolSchemasNeverExposeUserIdentity()
    {
        var tools = CreateAllTools();

        tools.Should().HaveCount(5);
        tools.Should().OnlyContain(tool => IsValidJson(tool.Definition.ParametersJsonSchema));
        tools.Should().OnlyContain(tool =>
            !tool.Definition.ParametersJsonSchema.Contains(
                "userId",
                StringComparison.OrdinalIgnoreCase)
            && !tool.Definition.ParametersJsonSchema.Contains(
                "ownerId",
                StringComparison.OrdinalIgnoreCase));
        tools.Where(tool => tool.Definition.IsMutation)
            .Select(tool => tool.Definition.Name)
            .Should().BeEquivalentTo(
                AgentToolNames.CreateBooking,
                AgentToolNames.CancelBooking);
    }

    private static IReadOnlyList<IAgentTool> CreateAllTools()
    {
        var room = TestData.CreateRoom(RoomCode.A, 4);
        var rooms = new FakeRoomRepository([room]);
        var bookings = new FakeBookingRepository();
        var currentUser = new FakeCurrentUser("User1");
        var timeProvider = new StubTimeProvider(TestData.Utc(9));

        return
        [
            new CreateBookingTool(new CreateBookingUseCase(currentUser, rooms, bookings)),
            new ListAvailableRoomsTool(new ListAvailableRoomsUseCase(rooms, bookings)),
            new GetRoomScheduleTool(new GetRoomScheduleUseCase(rooms, bookings)),
            new ListMyBookingsTool(new ListMyBookingsUseCase(
                currentUser,
                rooms,
                bookings,
                timeProvider)),
            new CancelBookingTool(new CancelBookingUseCase(
                currentUser,
                rooms,
                bookings,
                timeProvider))
        ];
    }

    private static bool ReadSuccess(AgentToolResult result)
    {
        using var document = JsonDocument.Parse(result.ContentJson);
        return document.RootElement.GetProperty("success").GetBoolean();
    }

    private static string? ReadCode(AgentToolResult result)
    {
        using var document = JsonDocument.Parse(result.ContentJson);
        return document.RootElement.GetProperty("code").GetString();
    }

    private static JsonElement ReadData(AgentToolResult result)
    {
        using var document = JsonDocument.Parse(result.ContentJson);
        return document.RootElement.GetProperty("data").Clone();
    }

    private static bool IsValidJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object;
    }
}
