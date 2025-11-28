using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers.SpeechLockCommandHandlers;
using VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;
using VoiceAssistant.Shared.Data.Entities;

namespace VoiceAssistant.Data.EntityFrameworkCore.Tests.CommandHandlers.SpeechLockCommandHandlers;

public class SpeechLockCreateCommandHandlerTests : IDisposable
{
    private readonly VoiceAssistantDbContext _context;
    private readonly SpeechLockCreateCommandHandler _handler;

    public SpeechLockCreateCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<VoiceAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VoiceAssistantDbContext(options);
        _handler = new SpeechLockCreateCommandHandler(_context);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateNewLock_WhenNoLockExists()
    {
        // Arrange
        var command = new SpeechLockCreateCommand();

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeGreaterThan(0);
        var locks = await _context.SpeechLocks.ToListAsync();
        locks.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteExistingLocks_WhenCreatingNew()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-1) });
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-2) });
        await _context.SaveChangesAsync();

        var command = new SpeechLockCreateCommand();

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeGreaterThan(0);
        var locks = await _context.SpeechLocks.ToListAsync();
        locks.Should().HaveCount(1);
        locks[0].Id.Should().Be(result);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNewLockId()
    {
        // Arrange
        var command = new SpeechLockCreateCommand();

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var createdLock = await _context.SpeechLocks.FirstOrDefaultAsync();
        createdLock.Should().NotBeNull();
        createdLock!.Id.Should().Be(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
