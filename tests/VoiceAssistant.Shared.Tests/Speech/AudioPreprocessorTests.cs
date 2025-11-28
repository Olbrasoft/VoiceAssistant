using Olbrasoft.VoiceAssistant.Shared.Speech;

namespace Olbrasoft.VoiceAssistant.Shared.Tests.Speech;

public class AudioPreprocessorTests
{
    [Fact]
    public void Constructor_Default_ShouldNotThrow()
    {
        // Act & Assert - should not throw
        var preprocessor = new AudioPreprocessor();
        Assert.NotNull(preprocessor);
    }

    [Fact]
    public void Constructor_WithNonExistentPath_ShouldNotThrow()
    {
        // Act & Assert - should not throw, falls back to generated filters
        var preprocessor = new AudioPreprocessor("/non/existent/path.bin");
        Assert.NotNull(preprocessor);
    }

    [Fact]
    public void ComputeMelSpectrogram_WithEmptyArray_ShouldReturnValidShape()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = Array.Empty<float>();

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert
        Assert.Equal(1, result.GetLength(0));   // Batch size
        Assert.Equal(80, result.GetLength(1));  // Mel bins
        Assert.Equal(3000, result.GetLength(2)); // Frames (30 seconds)
    }

    [Fact]
    public void ComputeMelSpectrogram_WithShortAudio_ShouldPadToExpectedFrames()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = new float[16000]; // 1 second at 16kHz

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert - should be padded to 30 seconds (3000 frames)
        Assert.Equal(3000, result.GetLength(2));
    }

    [Fact]
    public void ComputeMelSpectrogram_WithLongAudio_ShouldTrimToExpectedFrames()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = new float[16000 * 60]; // 60 seconds at 16kHz

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert - should be trimmed to 30 seconds (3000 frames)
        Assert.Equal(3000, result.GetLength(2));
    }

    [Fact]
    public void ComputeMelSpectrogram_WithSilence_ShouldReturnNormalizedValues()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = new float[16000 * 30]; // 30 seconds of silence

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert - all values should be normalized (Whisper normalization: (x + 4) / 4)
        for (int i = 0; i < result.GetLength(1); i++)
        {
            for (int j = 0; j < result.GetLength(2); j++)
            {
                Assert.False(float.IsNaN(result[0, i, j]));
                Assert.False(float.IsInfinity(result[0, i, j]));
            }
        }
    }

    [Fact]
    public void ComputeMelSpectrogram_OutputShape_ShouldMatchWhisperExpectations()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = new float[16000 * 10]; // 10 seconds

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert - Whisper expects [batch, n_mels, frames] = [1, 80, 3000]
        Assert.Equal(3, result.Rank);
        Assert.Equal(1, result.GetLength(0));   // Batch
        Assert.Equal(80, result.GetLength(1));  // Mel bins
        Assert.Equal(3000, result.GetLength(2)); // Frames
    }

    [Fact]
    public void ComputeMelSpectrogram_WithSineWave_ShouldProduceNonZeroOutput()
    {
        // Arrange
        var preprocessor = new AudioPreprocessor();
        var samples = new float[16000]; // 1 second
        
        // Generate 440Hz sine wave
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = MathF.Sin(2 * MathF.PI * 440 * i / 16000f) * 0.5f;
        }

        // Act
        var result = preprocessor.ComputeMelSpectrogram(samples);

        // Assert - should have some non-zero values
        bool hasNonZero = false;
        for (int i = 0; i < result.GetLength(1) && !hasNonZero; i++)
        {
            for (int j = 0; j < Math.Min(100, result.GetLength(2)) && !hasNonZero; j++)
            {
                if (Math.Abs(result[0, i, j]) > 0.001f)
                {
                    hasNonZero = true;
                }
            }
        }
        Assert.True(hasNonZero);
    }
}
