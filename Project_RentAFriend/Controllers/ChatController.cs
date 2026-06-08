using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.ChatDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/chat")]
    public class ChatController : Controller
    {
        private readonly DBManager? _dbManager;

        public ChatController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Получить или создать чат с другом
        /// </summary>
        [Route("getOrCreate/{friendId}")]
        [HttpPost]
        public async Task<ActionResult> GetOrCreateChat(
            [FromHeader(Name = "TOKEN")] string token,
            int friendId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Chats == null || _dbManager.Users == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                if (userId == friendId)
                {
                    return BadRequest(new { message = "Нельзя создать чат с самим собой" });
                }

                var friend = await _dbManager.Users
                    .FirstOrDefaultAsync(u => u.UserID == friendId && u.IsActive);

                if (friend == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Ищем существующий чат
                var chat = await _dbManager.Chats
                    .FirstOrDefaultAsync(c =>
                        (c.ClientID == userId && c.FriendID == friendId) ||
                        (c.ClientID == friendId && c.FriendID == userId));

                if (chat == null)
                {
                    // Определяем кто клиент, кто друг
                    var currentUser = await _dbManager.Users.FindAsync(userId);
                    var isCurrentUserClient = currentUser?.Role == "Client";

                    chat = new Chat
                    {
                        ClientID = isCurrentUserClient ? userId.Value : friendId,
                        FriendID = isCurrentUserClient ? friendId : userId.Value,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbManager.Chats.Add(chat);
                    await _dbManager.SaveChangesAsync();
                }

                var interlocutorId = chat.ClientID == userId ? chat.FriendID : chat.ClientID;
                var interlocutor = await _dbManager.Users.FindAsync(interlocutorId);

                return Ok(new
                {
                    message = chat.ChatID == 0 ? "Чат создан" : "Чат получен",
                    chatId = chat.ChatID,
                    interlocutor = new
                    {
                        id = interlocutor?.UserID,
                        name = interlocutor?.FullName,
                        role = interlocutor?.Role
                    },
                    createdAt = chat.CreatedAt,
                    isActive = chat.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить все чаты пользователя
        /// </summary>
        [Route("myChats")]
        [HttpGet]
        public async Task<ActionResult> GetMyChats(
            [FromHeader(Name = "TOKEN")] string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (_dbManager == null || _dbManager.Chats == null || _dbManager.Messages == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var query = _dbManager.Chats
                    .Include(c => c.Client)
                    .Include(c => c.Friend)
                    .Where(c => (c.ClientID == userId || c.FriendID == userId) && c.IsActive);

                var totalCount = await query.CountAsync();

                var chats = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        Chat = c,
                        LastMessage = _dbManager.Messages
                            .Where(m => m.ChatID == c.ChatID && !m.IsDeleted)
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => new { m.Content, m.CreatedAt })
                            .FirstOrDefault()
                    })
                    .OrderByDescending(x => x.LastMessage != null ? x.LastMessage.CreatedAt : x.Chat.CreatedAt)
                    .Select(x => new ChatListDTO
                    {
                        ChatID = x.Chat.ChatID,
                        InterlocutorID = x.Chat.ClientID == userId ? x.Chat.FriendID : x.Chat.ClientID,
                        InterlocutorName = x.Chat.ClientID == userId
                            ? (x.Chat.Friend != null ? x.Chat.Friend.FullName : "Unknown")
                            : (x.Chat.Client != null ? x.Chat.Client.FullName : "Unknown"),
                        LastMessageAt = x.LastMessage != null ? x.LastMessage.CreatedAt : x.Chat.CreatedAt,
                        CreatedAt = x.Chat.CreatedAt,
                        IsActive = x.Chat.IsActive,
                        LastMessage = x.LastMessage != null
                            ? (x.LastMessage.Content.Length > 50
                                ? x.LastMessage.Content.Substring(0, 50) + "..."
                                : x.LastMessage.Content)
                            : "Нет сообщений",
                        UnreadCount = _dbManager.Messages
                            .Count(m => m.ChatID == x.Chat.ChatID
                                        && m.SenderID != userId
                                        && !m.IsRead
                                        && !m.IsDeleted)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    message = "Чаты получены",
                    chats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить детали чата
        /// </summary>
        [Route("{chatId}")]
        [HttpGet]
        public async Task<ActionResult> GetChatDetails(
            [FromHeader(Name = "TOKEN")] string token,
            int chatId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Users == null || _dbManager.Chats == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var chat = await _dbManager.Chats
                    .Include(c => c.Client)
                    .Include(c => c.Friend)
                    .FirstOrDefaultAsync(c => c.ChatID == chatId &&
                        (c.ClientID == userId || c.FriendID == userId));

                if (chat == null)
                {
                    return NotFound(new { message = "Чат не найден" });
                }

                var interlocutorId = chat.ClientID == userId ? chat.FriendID : chat.ClientID;
                var interlocutor = await _dbManager.Users.FindAsync(interlocutorId);

                return Ok(new
                {
                    message = "Детали чата",
                    chat = new
                    {
                        chat.ChatID,
                        interlocutor = new
                        {
                            id = interlocutor?.UserID,
                            name = interlocutor?.FullName,
                            role = interlocutor?.Role
                        },
                        chat.CreatedAt,
                        chat.LastMessageAt,
                        chat.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Закрыть чат
        /// </summary>
        [Route("{chatId}/close")]
        [HttpPut]
        public async Task<ActionResult> CloseChat(
            [FromHeader(Name = "TOKEN")] string token,
            int chatId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Chats == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var chat = await _dbManager.Chats
                    .FirstOrDefaultAsync(c => c.ChatID == chatId &&
                        (c.ClientID == userId || c.FriendID == userId));

                if (chat == null)
                {
                    return NotFound(new { message = "Чат не найден" });
                }

                chat.IsActive = false;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Чат закрыт",
                    chatId = chat.ChatID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}