using System.Security.Cryptography;

namespace Core;

public static class EmergencyExitChallenge
{
    public const int PhraseLength = 24;
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string GeneratePhrase()
    {
        Span<char> buffer = stackalloc char[PhraseLength];
        for (int i = 0; i < PhraseLength; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(buffer);
    }

    public static bool Matches(string? expectedPhrase, string? actualInput)
    {
        if (string.IsNullOrEmpty(expectedPhrase) || string.IsNullOrEmpty(actualInput))
            return false;

        return string.Equals(expectedPhrase, actualInput.Trim(), StringComparison.Ordinal);
    }
}
