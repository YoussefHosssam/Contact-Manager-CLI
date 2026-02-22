using System.Text.RegularExpressions;

namespace ContactManager.Application;

public static class ContactValidator
{
    // Not perfect RFC, but good enough for internship task
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool Validate(string name, string phone, string email, out string error)
    {
        name = name.Trim();
        phone = phone.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name cannot be empty.";
            return false;
        }

        // Phone: allow +, spaces, -, () but must contain enough digits
        var digits = DigitsOnly(phone);
        if (digits.Length < 8)
        {
            error = "Phone number must contain at least 8 digits.";
            return false;
        }

        if (!EmailRegex.IsMatch(email))
        {
            error = "Email format is invalid.";
            return false;
        }

        error = "";
        return true;
    }

    public static string DigitsOnly(string s)
        => new string(s.Where(char.IsDigit).ToArray());
}