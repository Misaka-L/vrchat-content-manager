﻿namespace VRChatContentManager.ConnectCore.Models.ClientSession;

public record ChallengeSession(string Code, string ClientId, string IdentityPrompt, DateTimeOffset Expires);