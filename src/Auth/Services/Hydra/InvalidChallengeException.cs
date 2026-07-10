namespace Auth.Services.Hydra;

/// <summary>login / consent / logout challenge 無效（不存在、已被使用或已過期）時由 HydraService 拋出。</summary>
public class InvalidChallengeException() : Exception("Hydra challenge is invalid, used or expired.");
