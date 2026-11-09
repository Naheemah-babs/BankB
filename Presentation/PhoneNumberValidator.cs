
namespace TatumWelcome2.Presentation;

public static class PhoneNumberValidator
{
    public static bool IsValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        if (phone.Length != 11)
            return false;

        if (!phone.All(char.IsDigit))
            return false;

        string prefix = phone.Substring(0, 3);

        if (prefix != "070" &&
            prefix != "080" &&
            prefix != "081" &&
            prefix != "090" &&
            prefix != "091")
            return false;

        return true;
    }
}
