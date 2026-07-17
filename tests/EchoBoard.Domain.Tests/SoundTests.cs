using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Domain.Tests;

public sealed class SoundTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(".mp3")]
    [InlineData("WAV")]
    [InlineData(".ogg")]
    [InlineData("FLAC")]
    [InlineData(".m4a")]
    [InlineData("AAC")]
    public void CreateAcceptsSupportedExtensionsAndNormalizesValues(string extension)
    {
        var sound = Sound.Create(
            "  Intro  ",
            "  C:\\Audio\\intro.MP3  ",
            extension,
            TimeSpan.FromSeconds(3),
            12345,
            categoryId: null,
            sortOrder: 2,
            CreatedAt);

        sound.Name.Should().Be("Intro");
        sound.FilePath.Should().Be("C:\\Audio\\intro.MP3");
        sound.Extension.Should().Be(extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}");
        sound.Volume.Should().Be(1.0);
        sound.IsLoopEnabled.Should().BeFalse();
        sound.StopPreviousSound.Should().BeTrue();
        sound.AllowOverlap.Should().BeFalse();
        sound.WaveformPeaks.Should().BeEmpty();
        sound.CreatedAt.Should().Be(CreatedAt);
        sound.UpdatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void PlaybackAndWaveformMutationsPersistValidatedValues()
    {
        var sound = CreateValidSound();
        var updatedAt = CreatedAt.AddMinutes(1);
        var waveform = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

        sound.ConfigurePlayback(isLoopEnabled: true, stopPreviousSound: false, allowOverlap: true, updatedAt);
        sound.SetWaveformPeaks(waveform, updatedAt);

        sound.IsLoopEnabled.Should().BeTrue();
        sound.StopPreviousSound.Should().BeFalse();
        sound.AllowOverlap.Should().BeTrue();
        sound.WaveformPeaks.Should().Equal(waveform);
        sound.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void SetWaveformPeaksRejectsUnexpectedPeakCount()
    {
        var sound = CreateValidSound();

        var act = () => sound.SetWaveformPeaks([1, 2, 3], CreatedAt.AddMinutes(1));

        act.Should().Throw<DomainValidationException>().WithMessage("*32*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsBlankName(string name)
    {
        var act = () => Sound.Create(name, "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*Name*");
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".mp4")]
    public void CreateRejectsUnsupportedExtension(string extension)
    {
        var act = () => Sound.Create("Intro", "C:\\Audio\\intro.mp3", extension, TimeSpan.FromSeconds(1), 1, null, 0, CreatedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*extension*");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ChangeVolumeRejectsValuesOutsideRange(double volume)
    {
        var sound = CreateValidSound();
        var changedAt = CreatedAt.AddMinutes(1);

        var act = () => sound.ChangeVolume(volume, changedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*Volume*");
    }

    [Fact]
    public void MutationsUpdateExpectedFieldsAndUpdatedAt()
    {
        var sound = CreateValidSound();
        var categoryId = Guid.NewGuid();
        var updatedAt = CreatedAt.AddMinutes(5);

        sound.Rename("Renamed", updatedAt);
        sound.MoveToCategory(categoryId, updatedAt);
        sound.ChangeSortOrder(10, updatedAt);
        sound.ChangeVolume(0.5, updatedAt);
        sound.SetFavorite(true, updatedAt);
        sound.UpdateFileMetadata(".wav", TimeSpan.FromSeconds(8), 9876, updatedAt);

        sound.Name.Should().Be("Renamed");
        sound.CategoryId.Should().Be(categoryId);
        sound.SortOrder.Should().Be(10);
        sound.Volume.Should().Be(0.5);
        sound.IsFavorite.Should().BeTrue();
        sound.Extension.Should().Be(".wav");
        sound.Duration.Should().Be(TimeSpan.FromSeconds(8));
        sound.FileSize.Should().Be(9876);
        sound.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void CreateRejectsNonUtcTimestamp()
    {
        var localTime = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.FromHours(-3));

        var act = () => Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, localTime);

        act.Should().Throw<DomainValidationException>().WithMessage("*UTC*");
    }

    private static Sound CreateValidSound()
    {
        return Sound.Create("Intro", "C:\\Audio\\intro.mp3", ".mp3", TimeSpan.FromSeconds(1), 1, null, 0, CreatedAt);
    }
}
