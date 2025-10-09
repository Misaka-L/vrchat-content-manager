using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VRChatContentManager.ConnectCore.Models.ClientSession;

namespace VRChatContentManager.ConnectCore.Services;

public sealed class ClientSessionService(
    ILogger<ClientSessionService> logger,
    IRequestChallengeService requestChallengeService,
    ISessionStorageService sessionStorageService)
{
    private const string Issuer = "vrchat-content-manager";
    private const string Subject = "content-manager-build-pipeline-rpc";

    private const string SecretKey =
        "Ocxo643MhcRq2EDF58nx4u4UD6c1s9GGwvq57c8OO8G6WH2Ovi3E1080rFmlxJQHhiqg3980CCIw3443iPY084x2p0beRx278wrsG819zzAQup7x8v4VykPr714MX3Bl";

    private readonly Lock _challengeSessionLock = new();
    private readonly List<ChallengeSession> _challengeSessions = [];

    public async ValueTask<TokenValidationResult> ValidateJwtAsync(string jwt)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ValidIssuer = Issuer,
            ValidAlgorithms =
            [
                SecurityAlgorithms.HmacSha256
            ],
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireAudience = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            NameClaimType = JwtRegisteredClaimNames.Aud,
            AudienceValidator = (audiences, _, _) =>
            {
                foreach (var audience in audiences)
                {
                    if (sessionStorageService.GetSessionByClientId(audience) is { } session &&
                        session.Expires > DateTimeOffset.UtcNow)
                        return true;
                }

                return false;
            }
        });

        return result;
    }

    public async ValueTask<string> CreateChallengeAsync(string clientId)
    {
        await CleanupExpiredSessionsAsync();

        logger.LogInformation("Creating challenge for client {ClientId}", clientId);
        var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

        lock (_challengeSessionLock)
        {
            if (_challengeSessions.FirstOrDefault(challenge => challenge.ClientId == clientId) is { } session)
                _challengeSessions.Remove(session);

            var expires = DateTimeOffset.UtcNow.AddMinutes(5);

            var challengeSession = new ChallengeSession(code, clientId, expires);
            _challengeSessions.Add(challengeSession);
        }

        await requestChallengeService.RequestChallengeAsync(code, clientId);

        return code;
    }

    public async ValueTask<string> CreateSessionTask(string code, string clientId)
    {
        await CleanupExpiredSessionsAsync();

        logger.LogInformation("Creating session for client {ClientId}", clientId);
        lock (_challengeSessionLock)
        {
            if (_challengeSessions.FirstOrDefault(session => session.Code == code) is not { } challengeSession)
            {
                throw new InvalidOperationException("Invalid challenge code.");
            }

            _challengeSessions.Remove(challengeSession);
        }

        var expires = DateTimeOffset.UtcNow.AddHours(1);

        var session = new RpcClientSession(clientId, expires);
        await sessionStorageService.AddSessionAsync(session);

        return GenerateJwt(clientId);
    }

    public async ValueTask<string> RefreshSessionAsync(string clientId)
    {
        await CleanupExpiredSessionsAsync();

        logger.LogInformation("Refreshing session for client {ClientId}", clientId);
        if (sessionStorageService.GetSessionByClientId(clientId) is not { } existingSession)
        {
            throw new InvalidOperationException("No existing session found for the given client ID.");
        }

        await sessionStorageService.RemoveSessionByClientIdAsync(clientId);

        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var newSession = new RpcClientSession(clientId, expires);
        await sessionStorageService.AddSessionAsync(newSession);

        return GenerateJwt(clientId);
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var now = DateTimeOffset.UtcNow;

        await sessionStorageService.RemoveExpiredSessionsAsync();
        lock (_challengeSessionLock)
        {
            _challengeSessions.RemoveAll(session => session.Expires <= now);
        }
    }

    private string GenerateJwt(string clientId)
    {
        var currentDateTime = DateTimeOffset.UtcNow;

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Iss, Issuer),
            new(JwtRegisteredClaimNames.Sub, Subject),
            new(JwtRegisteredClaimNames.Aud, clientId),
            new(JwtRegisteredClaimNames.Exp, currentDateTime.AddMinutes(30).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Iat, currentDateTime.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, currentDateTime.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}