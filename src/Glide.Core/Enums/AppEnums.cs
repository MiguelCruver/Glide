namespace Glide.Core.Enums;

public enum GlideModule
{
    Home,
    Audio,
    Video,
    Notifications,
    Connect,
    Settings
}

public enum ServiceCategory
{
    Audio,
    Video,
    Communication,
    Notification
}

public enum AudioPlaybackState
{
    Stopped,
    Playing,
    Paused,
    Buffering
}

public enum ConnectState
{
    Disconnected,
    Connecting,
    InRoom,
    Reconnecting,
    Failed
}

