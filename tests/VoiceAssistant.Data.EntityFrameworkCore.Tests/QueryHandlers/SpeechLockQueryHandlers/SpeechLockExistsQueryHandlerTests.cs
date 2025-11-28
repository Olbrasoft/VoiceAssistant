using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore.QueryHandlers.SpeechLockQueryHandlers;
using VoiceAssistant.Shared.Data.Entities;
using VoiceAssistant.Shared.Data.Queries.SpeechLockQueries;

namespace VoiceAssistant.Data.EntityFrameworkCore.Tests.QueryHandlers.SpeechLockQueryHandlers;

public class SpeechLockExistsQueryHandlerTests : IDisposable
{
    private readonly VoiceAssistantDbContext _context;
    private readonly SpeechLockExistsQueryHandler _handler;

    public SpeechLockExistsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<VoiceAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VoiceAssistantDbContext(options);
        _handler = new SpeechLockExistsQueryHandler(_context);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTrue_WhenRecentLockExists()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var query = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFalse_WhenNoLocksExist()
    {
        // Arrange
        var query = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFalse_WhenOnlyOldLocksExist()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
        await _context.SaveChangesAsync();

        var query = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldCleanupOldLocks_WhenNoActiveLockExists()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-15) });
        await _context.SaveChangesAsync();

        var query = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };

        // Act
        await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        var remainingLocks = await _context.SpeechLocks.ToListAsync();
        remainingLocks.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCleanupOldLocks_WhenActiveLockExists()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow }); // Active
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-10) }); // Old
        await _context.SaveChangesAsync();

        var query = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };

        // Act
        var result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var remainingLocks = await _context.SpeechLocks.ToListAsync();
        remainingLocks.Should().HaveCount(2); // Both should still exist
    }

    [Fact]
    public async Task HandleAsync_ShouldRespectMaxAgeMinutesParameter()
    {
        // Arrange
        _context.SpeechLocks.Add(new SpeechLockEntity { CreatedAt = DateTime.UtcNow.AddMinutes(-3) });
        await _context.SaveChangesAsync();

        // Act & Assert - 5 minutes max age, lock is 3 minutes old -> active
        var query5Min = new SpeechLockExistsQuery { MaxAgeMinutes = 5 };
        var result5Min = await _handler.HandleAsync(query5Min, CancellationToken.None);
        result5Min.Should().BeTrue();

        // Act & Assert - 2 minutes max age, lock is 3 minutes old -> expired
        var query2Min = new SpeechLockExistsQuery { MaxAgeMinutes = 2 };
        var result2Min = await _handler.HandleAsync(query2Min, CancellationToken.None);
        result2Min.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseDefaultMaxAge_WhenNotSpecified()
    {
        // Arrange
        var query = new SpeechLockExistsQuery();

        // Assert
        query.MaxAgeMinutes.Should().Be(5);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
