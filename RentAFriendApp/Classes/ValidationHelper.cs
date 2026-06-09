using System.Text.RegularExpressions;

namespace RentAFriendApp.Classes
{
    public static class ValidationHelper
    {
        public static bool IsValidRegexPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            string pattern = @"^(?:\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}|\d{11})$";
            return Regex.IsMatch(phone.Trim(), pattern);
        }
        public static string ValidPhoneText(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "⚠ Минимальный размер 11 символов";
            }
            if (!IsValidRegexPhone(phone))
            {
                return "⚠ Неверный формат номера телефона";
            }
            return ("✓");
        }
        public static string ValidationFullName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "⚠ Обязательное поле";
            if (name.Length < 2) return "⚠ Минимум 2 символа";
            if (name.Length < 2) return "⚠ Максимум 100 символа";
            return "✓";
        }
        public static string HourlyRateTextValidation(string text, decimal? hourlyRate)
        {
            if (string.IsNullOrEmpty(text)) return "⚠ Обязательное поле";
            if (hourlyRate.HasValue)
            {
                if (hourlyRate.Value <= 0) return "⚠ Должна быть > 0";
                if (hourlyRate.Value > 10000) return "⚠ Максимум 10 000";
            }
            else
            {
                return "⚠ Введите корректное значение";
            }
            return "✓";
        }
    }
}
