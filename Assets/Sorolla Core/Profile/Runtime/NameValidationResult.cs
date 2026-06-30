namespace Sorolla.Profile
{
    public enum NameValidationResult
    {
        Ok = 0,
        Empty = 1,
        TooShort = 2,
        TooLong = 3,
        Blocked = 4,
        Invalid = 5     // contains control / zero-width / bidi-control characters
    }
}
