using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using VRChatContentManager.ConnectCore.Models.ClientSession;

namespace VRChatContentManager.ConnectCore.Services;

public sealed class ClientSessionService(ILogger<ClientSessionService> logger)
{
    private const string Issuer = "vrchat-content-manager";
    private const string Subject = "content-manager-build-pipeline-rpc";

    private const string SecretKey =
        "Ocxo643MhcRq2EDF58nx4u4UD6c1s9GGwvq57c8OO8G6WH2Ovi3E1080rFmlxJQHhiqg3980CCIw3443iPY084x2p0beRx278wrsG819zzAQup7x8v4VykPr714MX3Bl";

    private readonly Lock _sessionLock = new();

    private readonly List<RpcClientSession> _sessions = [];
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
                return audiences.Any(audience =>
                    _sessions.Any(session =>
                        session.ClientId == audience &&
                        session.Expires > DateTimeOffset.UtcNow
                    )
                );
            }
        });

        return result;
    }

    public string CreateChallenge(string clientId)
    {
        CleanupExpiredSessions();
        
        logger.LogInformation("Creating challenge for client {ClientId}", clientId);
        lock (_sessionLock)
        {
            if (_challengeSessions.FirstOrDefault(challenge => challenge.ClientId == clientId) is { } session)
                _challengeSessions.Remove(session);

            var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            var expires = DateTimeOffset.UtcNow.AddMinutes(5);

            var challengeSession = new ChallengeSession(code, clientId, expires);
            _challengeSessions.Add(challengeSession);
            logger.LogInformation("Created challenge code {Code} for client {ClientId}", code, clientId);

            return code;
        }
    }

    public string CreateSession(string code, string clientId)
    {
        CleanupExpiredSessions();

        logger.LogInformation("Creating session for client {ClientId}", clientId);
        lock (_sessionLock)
        {
            if (_challengeSessions.FirstOrDefault(session => session.Code == code) is not { } challengeSession)
            {
                throw new InvalidOperationException("Invalid challenge code.");
            }

            _challengeSessions.Remove(challengeSession);

            var expires = DateTimeOffset.UtcNow.AddHours(1);

            var session = new RpcClientSession(clientId, expires);
            _sessions.Add(session);

            return GenerateJwt(clientId);
        }
    }

    public string RefreshSession(string clientId)
    {
        CleanupExpiredSessions();
        
        logger.LogInformation("Refreshing session for client {ClientId}", clientId);
        lock (_sessionLock)
        {
            if (_sessions.FirstOrDefault(session => session.ClientId == clientId) is not { } existingSession)
            {
                throw new InvalidOperationException("No existing session found for the given client ID.");
            }

            _sessions.Remove(existingSession);

            var expires = DateTimeOffset.UtcNow.AddHours(1);

            var newSession = new RpcClientSession(clientId, expires);
            _sessions.Add(newSession);

            return GenerateJwt(clientId);
        }
    }

    private void CleanupExpiredSessions()
    {
        lock (_sessionLock)
        {
            var now = DateTimeOffset.UtcNow;

            _sessions.RemoveAll(session => session.Expires <= now);
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