using EchoBoard.Domain.Entities;
using EchoBoard.Domain.Enums;
using EchoBoard.Domain.Exceptions;
using EchoBoard.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace EchoBoard.Domain.Tests;

public sealed class HotkeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Alt, "s", "Ctrl+Alt+S")]
    [InlineData(HotkeyModifiers.Shift, "f9", "Shift+F9")]
    [InlineData(HotkeyModifiers.Alt | HotkeyModifiers.Windows, "1", "Alt+Win+1")]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Windows, "pagedown", "Ctrl+Shift+Win+PageDown")]
    public void CombinationNormalizesModifierOrderAndPrimaryKey(HotkeyModifiers modifiers, string primaryKey, string expected)
    {
        var combination = HotkeyCombination.Create(modifiers, primaryKey);

        combination.NormalizedText.Should().Be(expected);
    }

    [Theory]
    [InlineData(HotkeyModifiers.None, "S", "modifier")]
    [InlineData(HotkeyModifiers.Control, "", "Primary key")]
    [InlineData(HotkeyModifiers.Control, "Esc", "not supported")]
    [InlineData(HotkeyModifiers.Control, "Space", "not supported")]
    [InlineData(HotkeyModifiers.Control, "OemPlus", "not supported")]
    public void CombinationRejectsInvalidValues(HotkeyModifiers modifiers, string primaryKey, string messagePart)
    {
        var act = () => HotkeyCombination.Create(modifiers, primaryKey);

        act.Should().Throw<DomainValidationException>().WithMessage($"*{messagePart}*");
    }

    [Fact]
    public void SoundBindingRequiresSoundTargetOnly()
    {
        var combination = HotkeyCombination.Create(HotkeyModifiers.Control, "S");

        var binding = HotkeyBinding.CreateForSound(Guid.NewGuid(), combination, isEnabled: true, Now);

        binding.TargetKind.Should().Be(HotkeyBindingTargetKind.Sound);
        binding.SoundId.Should().NotBeNull();
        binding.GlobalCommand.Should().BeNull();
        binding.NormalizedKeyCombination.Should().Be("Ctrl+S");
        binding.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void GlobalCommandBindingRequiresGlobalCommandTargetOnly()
    {
        var combination = HotkeyCombination.Create(HotkeyModifiers.Alt, "F10");

        var binding = HotkeyBinding.CreateForGlobalCommand(GlobalHotkeyCommand.ShowHideMainWindow, combination, isEnabled: false, Now);

        binding.TargetKind.Should().Be(HotkeyBindingTargetKind.GlobalCommand);
        binding.SoundId.Should().BeNull();
        binding.GlobalCommand.Should().Be(GlobalHotkeyCommand.ShowHideMainWindow);
        binding.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void BindingMutationsValidateUtcAndUpdateCombination()
    {
        var binding = HotkeyBinding.CreateForGlobalCommand(
            GlobalHotkeyCommand.StopAllSounds,
            HotkeyCombination.Create(HotkeyModifiers.Control, "F8"),
            isEnabled: true,
            Now);
        var changedAt = Now.AddMinutes(1);

        binding.ChangeCombination(HotkeyCombination.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "F8"), changedAt);
        binding.SetEnabled(false, changedAt);

        binding.NormalizedKeyCombination.Should().Be("Ctrl+Alt+F8");
        binding.IsEnabled.Should().BeFalse();
        binding.UpdatedAt.Should().Be(changedAt);
    }
}
