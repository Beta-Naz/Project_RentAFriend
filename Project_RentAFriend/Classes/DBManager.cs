using Microsoft.EntityFrameworkCore;
using Project_RentAFriend.Models;

namespace Project_RentAFriend.Classes
{
    public class DBManager : DbContext
    {
        public DbSet<User>? Users { get; set; }
        public DbSet<FriendProfile>? FriendProfiles { get; set; }
        public DbSet<Booking>? Bookings { get; set; }
        public DbSet<AuditLog>? AuditLogs { get; set; }
        public DbSet<Message>? Messages { get; set; }
        public DbSet<Chat>? Chats { get; set; }
        public DbSet<Review>? Reviews { get; set; }
        public DbSet<Schedule>? Schedules { get; set; }
        public DbSet<Notification>? Notifications { get; set; }
        public DBManager() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(Config.ConnectionString,Config.CurrentVersion);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            //1. User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                
                entity.HasIndex(e => e.Email)
                    .IsUnique(); // Email должен быть уникальным
                
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Role)
                    .HasDefaultValue("User");
                
                // 🔧 Глобальный фильтр
                entity.HasQueryFilter(e => e.IsActive);
            });
            
            //2. FriendProfile
            modelBuilder.Entity<FriendProfile>(entity =>
            {
                entity.HasKey(e => e.ProfileID);
                
                //Один пользователь = один профиль
                entity.HasIndex(e => e.UserID)
                    .IsUnique(); // Уникальный ключ!
                
                entity.HasOne(e => e.User)
                    .WithOne()  // Один-к-одному
                    .HasForeignKey<FriendProfile>(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade); // Удалили User -> удалили Profile
                
                entity.Property(e => e.HourlyRate)
                    .HasColumnType("decimal(18,2)");
                
                entity.Property(e => e.AverageRating)
                    .HasColumnType("decimal(3,2)");
            });
            
            // 3. Schedule
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleID);
                
                entity.HasOne(e => e.Booking)
                    .WithOne()  // Schedule → Booking (один-к-одному)
                    .HasForeignKey<Schedule>(e => e.BookingID)
                    .OnDelete(DeleteBehavior.SetNull); // Удалили Booking -> Schedule.BookingID = null
                
                // 🔧 Индекс для быстрого поиска доступных слотов
                entity.HasIndex(e => new { e.ProfileID, e.Date, e.StartTime, e.IsAvailable })
                    .HasDatabaseName("IX_Schedule_Search");
            });
            
            //4. Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingID);
                
                // Связь с Schedule
                entity.HasOne(e => e.Schedule)
                    .WithOne(s => s.Booking)
                    .HasForeignKey<Booking>(e => e.ScheduleID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Связь с Review
                entity.HasOne(e => e.Review)
                    .WithOne(r => r.Booking)
                    .HasForeignKey<Review>(r => r.BookingID)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(e => e.Status)
                    .HasDefaultValue("Pending");
                
                entity.Property(e => e.PaymentStatus)
                    .HasDefaultValue("Pending");
                
                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");
                
                // 🔧 Индексы
                entity.HasIndex(e => e.ClientID);
                entity.HasIndex(e => e.FriendProfileID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.ClientID, e.Status });
            });
            
            // ========== 5. Chat ==========
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.ChatID);
                
                // Уникальный чат между Client и Friend
                entity.HasIndex(e => new { e.ClientID, e.FriendID })
                    .IsUnique();
                
                entity.HasOne(e => e.Client)
                    .WithMany()
                    .HasForeignKey(e => e.ClientID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Friend)
                    .WithMany()
                    .HasForeignKey(e => e.FriendID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // 🔧 Автообновление LastMessageAt (через триггер или в коде)
            });
            
            // ========== 6. Message ==========
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                
                entity.HasOne(e => e.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ChatID)
                    .OnDelete(DeleteBehavior.Cascade); // Удалили чат -> удалили сообщения
                
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderID)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Booking)
                    .WithMany()
                    .HasForeignKey(e => e.BookingID)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.Property(e => e.MessageType)
                    .HasDefaultValue("Text");
                
                // 🔧 Индексы
                entity.HasIndex(e => e.ChatID);
                entity.HasIndex(e => e.SenderID);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
            });
            
            //7. Review
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewID);
                
                // Один отзыв на одно бронирование
                entity.HasIndex(e => e.BookingID)
                    .IsUnique();
                
                entity.Property(e => e.Rating)
                    .IsRequired();
                
                entity.Property(e => e.IsApproved)
                    .HasDefaultValue(false);
                
                // 🔧 Индекс для модерации
                entity.HasIndex(e => new { e.IsApproved, e.CreatedAt });
            });
            
            //8. Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationID);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(e => e.Type)
                    .HasDefaultValue("Info");
                
                // 🔧 Индексы
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => new { e.UserID, e.IsRead, e.CreatedAt });
            });
            
            //9. AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.LogID);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.TableName)
                    .IsRequired()
                    .HasMaxLength(100);

                // 🔧 Индексы
                entity.HasIndex(e => e.LoggedAt);
                entity.HasIndex(e => new { e.TableName, e.RecordID });
                entity.HasIndex(e => e.UserID);
            });
        }
    }
}
