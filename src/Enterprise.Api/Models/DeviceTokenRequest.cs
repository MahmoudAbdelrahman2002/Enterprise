namespace Enterprise.Api.Models;

public sealed record DeviceTokenRequest(string Token, string? Platform = null);
