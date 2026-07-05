namespace EchoBoard.App.Controls;

public enum DeviceStatusKind
{
    Connected,
    Disconnected,
    Unavailable,
    Warning,
    Loading,
    Unknown
}

public enum AudioLevelMeterVariant
{
    Microphone,
    Effects,
    Monitor,
    VirtualOutput
}

public enum ToastNotificationKind
{
    Success,
    Info,
    Warning,
    Error
}
