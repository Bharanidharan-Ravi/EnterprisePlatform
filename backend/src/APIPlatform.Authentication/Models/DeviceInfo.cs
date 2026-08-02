namespace APIPlatform.Authentication.Models;

public sealed class DeviceInfo
{
    public string? DeviceId { get; init; }
    public string? ClientIp { get; init; }
    public string? UserAgent { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }
}
