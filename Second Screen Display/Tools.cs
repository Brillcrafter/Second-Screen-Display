namespace ClientPlugin;

public class Tools
{
    public static string ToHexStringLower(byte[] data)
    {
        var hexChars = new char[data.Length * 2];

        for (var i = 0; i < data.Length; i++)
        {
            var b = data[i];
            hexChars[i * 2] = GetHexValue(b >> 4);
            hexChars[i * 2 + 1] = GetHexValue(b & 0x0F);
        }

        return new string(hexChars);
    }

    private static char GetHexValue(int value)
    {
        // Maps 0-15 to '0'-'9' and 'a'-'f'
        return value < 10 ? (char)(value + '0') : (char)(value - 10 + 'a');
    }

}