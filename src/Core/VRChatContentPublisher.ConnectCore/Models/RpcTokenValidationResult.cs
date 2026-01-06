using Microsoft.IdentityModel.Tokens;

namespace VRChatContentPublisher.ConnectCore.Models;

public record RpcTokenValidationResult(TokenValidationResult TokenValidationResult, string ClientId, string ClientName);