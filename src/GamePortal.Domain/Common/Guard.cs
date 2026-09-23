namespace GamePortal.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} 는 비어 있을 수 없습니다.", paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{paramName} 는 {maxLength}자를 넘을 수 없습니다.", paramName);
        }

        return trimmed;
    }
}
