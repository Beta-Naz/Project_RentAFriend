using Microsoft.IdentityModel.Tokens;
using Project_RentAFriend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project_RentAFriend.Classes
{
    /// <summary>
    /// Отвечает за генерацию и валидацию токенов
    /// </summary>
    public class JwtToken
    {
        /// <summary>
        /// Секретный ключ для подписи токенов
        /// static означает, что ключ общий для всех экземпляров класса
        /// </summary>
        static byte[] Key = Encoding.UTF8.GetBytes("RentAFriendTheBestKursovoiProject!!!");

        /// <summary>
        /// Генерирует JWT токен для пользователя
        /// </summary>
        /// <param name="user">Пользователь, для которого создается токен</param>
        /// <returns>Строка с JWT токеном</returns>
        public static string Generate(User user)
        {
            JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("UserId", user.UserID.ToString())
            }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };
            SecurityToken Token = TokenHandler.CreateToken(tokenDescriptor);
            return TokenHandler.WriteToken(Token);
        }

        /// <summary>
        /// Извлекает ID пользователя из JWT токена
        /// </summary>
        /// <param name="token">JWT токен в виде строки</param>
        /// <returns>ID пользователя или null, если токен недействителен</returns>
        public static int? GetUserIdFromToken(string token)
        {
            try
            {
                JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();

                TokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken ValidatedToken);
                JwtSecurityToken JwtToken = (JwtSecurityToken)ValidatedToken;
                string UserId = JwtToken.Claims.First(x => x.Type == "UserId").Value;
                return int.Parse(UserId);
            }
            catch
            {
                return null;
            }
        }
    }
}
