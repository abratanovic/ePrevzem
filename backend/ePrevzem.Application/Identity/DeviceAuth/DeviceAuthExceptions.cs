namespace ePrevzem.Application.Identity.DeviceAuth;

public sealed class OnboardingCodeNotFoundException() : Exception("Onboarding code not found.");

public sealed class OnboardingCodeExpiredException() : Exception("Onboarding code expired or already redeemed.");

public sealed class DeviceNotFoundException() : Exception("Device not found or revoked.");

public sealed class InvalidChallengeException() : Exception("Challenge invalid, expired, or already used.");

public sealed class InvalidSignatureException() : Exception("Signature verification failed.");
