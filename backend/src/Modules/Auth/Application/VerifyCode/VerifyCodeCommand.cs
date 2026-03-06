namespace MoneyTracker.Modules.Auth.Application.VerifyCode;

public sealed record VerifyCodeCommand(string Email, string ChallengeToken);
