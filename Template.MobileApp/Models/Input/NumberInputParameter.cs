namespace Template.MobileApp.Models.Input;

public sealed class NumberInputParameter
{
    public string Title { get; }

    public string Value { get; }

    public int MaxLength { get; }

    public bool Digits { get; }

    public bool Masked { get; }

    public NumberInputParameter(string title, string value, int maxLength, bool digits = false, bool masked = false)
    {
        Title = title;
        Value = value;
        MaxLength = maxLength;
        Digits = digits;
        Masked = masked;
    }
}
