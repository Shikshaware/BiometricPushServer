using System;
using System.Text;

namespace BiometricPushServer.Common.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

        public static string ToBase64(this string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        public static string FromBase64(this string value) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(value));

        public static string Truncate(this string value, int maxLength) =>
            value?.Length > maxLength ? value[..maxLength] : value ?? string.Empty;
    }

    public static class DateTimeExtensions
    {
        public static bool IsToday(this DateTime dt) =>
            dt.Date == DateTime.Today;

        public static string ToDisplayString(this DateTime dt) =>
            dt.ToString("dd-MMM-yyyy HH:mm:ss");

        public static bool IsWithin(this DateTime dt, int seconds) =>
            (DateTime.UtcNow - dt).TotalSeconds <= seconds;
    }
}
