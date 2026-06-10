using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Classes;
using Project_RentAFriend.Models;
using Project_RentAFriend.Models.ClassesDTO.MessageDTO;

namespace Project_RentAFriend.Controllers
{
    [Route("/message")]
    public class MessageController : Controller
    {
        private readonly DBManager? _dbManager;

        public MessageController()
        {
            _dbManager = new DBManager();
        }

        /// <summary>
        /// Получить сообщения чата
        /// </summary>
        [Route("getByChat/{chatId}")]
        [HttpGet]
        public async Task<ActionResult> GetMessages(
            [FromHeader(Name = "TOKEN")] string token,
            int chatId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null || _dbManager.Chats == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                // Проверяем доступ к чату
                var chat = await _dbManager.Chats
                    .FirstOrDefaultAsync(c => c.ChatID == chatId &&
                        (c.ClientID == userId || c.FriendID == userId) &&
                        c.IsActive);

                if (chat == null)
                {
                    return NotFound(new { message = "Чат не найден или доступ запрещен" });
                }

                var query = _dbManager.Messages
                    .Where(m => m.ChatID == chatId && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt);

                var totalCount = await query.CountAsync();
                var messages = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageDTO
                    {
                        MessageID = m.MessageID,
                        SenderID = m.SenderID,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        IsRead = m.IsRead,
                        IsEdited = m.IsEdited,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                // Помечаем сообщения как прочитанные
                await MarkMessagesAsRead(chatId, userId.Value);

                return Ok(new
                {
                    message = "Сообщения получены",
                    messages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        [Route("send")]
        [HttpPost]
        public async Task<ActionResult> SendMessage(
            [FromHeader(Name = "TOKEN")] string token,
            [FromBody] SendMessageDTO messageData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null || _dbManager.Chats == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var chat = await _dbManager.Chats
                    .FirstOrDefaultAsync(c => c.ChatID == messageData.ChatID &&
                        (c.ClientID == userId || c.FriendID == userId) &&
                        c.IsActive);

                if (chat == null)
                {
                    return NotFound(new { message = "Чат не найден или доступ запрещен" });
                }

                if (string.IsNullOrWhiteSpace(messageData.Content))
                {
                    return BadRequest(new { message = "Сообщение не может быть пустым" });
                }

                if (messageData.Content.Length > 5000)
                {
                    return BadRequest(new { message = "Сообщение слишком длинное (максимум 5000 символов)" });
                }

                var message = new Message
                {
                    ChatID = messageData.ChatID,
                    SenderID = userId.Value,
                    Content = messageData.Content,
                    MessageType = messageData.MessageType ?? "Text",
                    IsRead = false,
                    IsEdited = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbManager.Messages.Add(message);
                chat.LastMessageAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Сообщение отправлено",
                    messageId = message.MessageID,
                    sentAt = message.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Редактировать сообщение
        /// </summary>
        [Route("edit/{messageId}")]
        [HttpPut]
        public async Task<ActionResult> EditMessage(
            [FromHeader(Name = "TOKEN")] string token,
            int messageId,
            [FromBody] EditMessageDTO editData)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var message = await _dbManager.Messages
                    .FirstOrDefaultAsync(m => m.MessageID == messageId);

                if (message == null)
                {
                    return NotFound(new { message = "Сообщение не найдено" });
                }

                if (message.SenderID != userId)
                {
                    return Forbid("Только автор может редактировать сообщение");
                }

                if (string.IsNullOrWhiteSpace(editData.Content))
                {
                    return BadRequest(new { message = "Сообщение не может быть пустым" });
                }

                message.Content = editData.Content;
                message.IsEdited = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Сообщение отредактировано",
                    messageId = message.MessageID,
                    newContent = message.Content
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Удалить сообщение (мягкое удаление)
        /// </summary>
        [Route("delete/{messageId}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteMessage(
            [FromHeader(Name = "TOKEN")] string token,
            int messageId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var message = await _dbManager.Messages
                    .Include(m => m.Chat)
                    .FirstOrDefaultAsync(m => m.MessageID == messageId);

                if (message == null)
                {
                    return NotFound(new { message = "Сообщение не найдено" });
                }

                var isAuthor = message.SenderID == userId;
                var isParticipant = message.Chat != null &&
                    (message.Chat.ClientID == userId || message.Chat.FriendID == userId);

                if (!isAuthor && !isParticipant)
                {
                    return Forbid("У вас нет прав на удаление этого сообщения");
                }

                message.IsDeleted = true;
                message.Content = "[Сообщение удалено]";
                message.UpdatedAt = DateTime.UtcNow;
                await _dbManager.SaveChangesAsync();

                return Ok(new
                {
                    message = "Сообщение удалено",
                    messageId = message.MessageID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Получить количество непрочитанных сообщений
        /// </summary>
        [Route("unreadCount")]
        [HttpGet]
        public async Task<ActionResult> GetUnreadCount([FromHeader(Name = "TOKEN")] string token)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var unreadCount = await _dbManager.Messages
                    .Include(m => m.Chat)
                    .Where(m => m.Chat != null &&
                                (m.Chat.ClientID == userId || m.Chat.FriendID == userId) &&
                                m.SenderID != userId &&
                                !m.IsRead &&
                                !m.IsDeleted)
                    .CountAsync();

                return Ok(new
                {
                    unreadCount,
                    hasUnread = unreadCount > 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Пометить сообщения как прочитанные
        /// </summary>
        [Route("markAsRead/{chatId}")]
        [HttpPut]
        public async Task<ActionResult> MarkMessagesAsRead(
            [FromHeader(Name = "TOKEN")] string token,
            int chatId)
        {
            try
            {
                if (_dbManager == null || _dbManager.Messages == null)
                {
                    return StatusCode(500, new { message = "Ошибка базы данных" });
                }

                int? userId = JwtToken.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var updatedCount = await _dbManager.Messages
                    .Where(m => m.ChatID == chatId && m.SenderID != userId && !m.IsRead && !m.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.IsRead, true)
                        .SetProperty(m => m.ReadAt, DateTime.UtcNow));

                return Ok(new
                {
                    message = "Сообщения отмечены как прочитанные",
                    updatedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
        private async Task MarkMessagesAsRead(int chatId, int userId)
        {
            if(_dbManager == null || _dbManager.Messages == null)
            {
                return;
            }
            await _dbManager.Messages
                .Where(m => m.ChatID == chatId && m.SenderID != userId && !m.IsRead && !m.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IsRead, true)
                    .SetProperty(m => m.ReadAt, DateTime.UtcNow));
        }
        [Route("recent")]
        [HttpGet]
        public async Task<ActionResult> GetRecentMessages(
    [FromHeader(Name = "TOKEN")] string token,
    [FromQuery] int count = 50)
        {
            try
            {
                if (_dbManager?.Messages == null || _dbManager.Users == null)
                    return StatusCode(500, new { message = "Ошибка базы данных" });

                int? adminId = JwtToken.GetUserIdFromToken(token);
                if (adminId == null)
                    return Unauthorized(new { message = "Недействительный токен" });

                var admin = await _dbManager.Users.FindAsync(adminId);
                if (admin == null || admin.Role != "Admin")
                    return Forbid("Доступ запрещен");

                var messages = await _dbManager.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Chat)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(count)
                    
                    .Select(m => new
                    {
                        m.MessageID,
                        m.ChatID,
                        m.SenderID,
                        SenderName = m.Sender != null ? m.Sender.FullName : "",
                        m.Content,
                        m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { message = "Сообщения получены", messages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.Message });
            }
        }
    }
}