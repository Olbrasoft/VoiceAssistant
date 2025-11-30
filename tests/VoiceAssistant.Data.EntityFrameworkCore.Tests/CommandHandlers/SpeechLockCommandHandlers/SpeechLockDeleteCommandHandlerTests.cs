using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers.SpeechLockCommandHandlers;
using VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;
using VoiceAssistant.Shared.Data.Entities;
using Xunit;

namespace VoiceAssistant.Data.EntityFrameworkCore.Tests.CommandHandlers.SpeechLockCommandHandlers;

public class SpeechLockDeleteCommandHandlerTests : IDisposable
{
    private readonly VoiceAssistantDbContext _context;
    private readonly SpeechLockDeleteCommandHandler _handler;

    public SpeechLockDeleteCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<VoiceAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VoiceAssistantDbContext(options);
        _handler = new SpeechLockDeleteCommandHandler(_context);
    }

    [Fact]
    public async Task HandleAsync_WithId_ShouldDeleteSpecificLock()
    {
        // Arrange
        var lock1 = new SpeechLockEntity { CreatedAt = DateTime.UtcNow };
        var lock2 = new SpeechLockEntity { CreatedAt = DateTime.UtcNow };
        _context.SpeechLocks.AddRange(lock1, lock2);
        await _context.SaveChangesAsync();

        var command = new SpeechLockDeleteCommand { Id = lock1.Id };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var remainingLocks = await _context.SpeechLocks.ToListAsync();
        remainingLocks.Should().HaveCount(1);
        remainingLocks[0].Id.Should().Be(lock2.Id);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var command = new SpeechLockDeleteCommand { Id = 999 };

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithoutId_ShouldDeleteAllLocks()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow });
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow });
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var command = new SpeechLockDeleteCommand(); // Id = 0

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var remainingLocks = await _context.SpeechLocks.ToListAsync();
        remainingLocks.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithoutId_WhenNoLocks_ShouldReturnFalse()
    {
        // Arrange
        var command = new SpeechLockDeleteCommand();

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
