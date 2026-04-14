namespace VirtualScreen.Core;

public enum AppEventType { Info, Success, Warning, Error }

public record AppEvent(AppEventType Type, string Message);
