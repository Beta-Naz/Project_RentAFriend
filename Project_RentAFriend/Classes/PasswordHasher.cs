namespace Project_RentAFriend.Classes
{
    public class PasswordHasher
    {
        /// <summary>
        /// Создает хэш пароля
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <returns>Хэш пароля</returns>
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Проверяет пароль
        /// </summary>
        /// <param name="password">Введенный пароль</param>
        /// <param name="hash">Хэш из БД</param>
        /// <returns>true - пароль верный, false - неверный</returns>
        public static bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
