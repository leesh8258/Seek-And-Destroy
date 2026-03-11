using System.Security.Cryptography;
using System.Text;

public static class RoomCodeService
{
    private const int CodeLength = 5;
    private const string CharSet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string GenerateRoomCode()
    {
        char[] chars = new char[CodeLength];
        byte[] buffer = new byte[16];
        int index = 0;

        using (RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create())
        {
            while (index < CodeLength)
            {
                randomGenerator.GetBytes(buffer);

                for (int i = 0; i < buffer.Length && index < CodeLength; i++)
                {
                    int value = buffer[i];

                    if (value >= 252)
                    {
                        continue;
                    }

                    int charIndex = value % CharSet.Length;
                    chars[index] = CharSet[charIndex];
                    index += 1;
                }
            }
        }

        return new string(chars);
    }

    public static bool NormalizeRoomCode(string raw, out string normalizeCode, out string error)
    {
        normalizeCode = string.Empty;
        error = string.Empty;

        if (raw == null)
        {
            error = "방 코드를 입력해주세요";
            return false;
        }

        string trimText = raw.Trim();
        if (trimText.Length == 0)
        {
            error = "방 코드를 입력해주세요";
            return false;
        }

        StringBuilder stringBuilder = new StringBuilder(trimText.Length);
        for (int i = 0; i < trimText.Length; i++)
        {
            char c = trimText[i];
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
            {
                continue;
            }

            stringBuilder.Append(char.ToUpperInvariant(c));
        }

        if (stringBuilder.Length != CodeLength)
        {
            error = "코드 5글자를 작성해주세요";
            return false;
        }

        for (int i = 0; i < stringBuilder.Length; i++)
        {
            char c = stringBuilder[i];
            bool isDigit = c >= '0' && c <= '9';
            bool isUpper = c >= 'A' && c <= 'Z';

            if (!isDigit && !isUpper)
            {
                error = "올바른 문자를 작성해 주세요";
                return false;
            }
        }

        normalizeCode = stringBuilder.ToString();
        return true;
    }
}
