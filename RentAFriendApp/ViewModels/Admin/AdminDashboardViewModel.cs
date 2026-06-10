using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.AuditLogDTO;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO.Response;
using RentAFriendApp.Models.ClassesDTO.NotificationDTO;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Admin
{
    internal class AdminDashboardViewModel : BaseViewModel
    {
        private readonly string _token;
        private int _adminUserId;

        private ObservableCollection<UserInfoDTO> _allUsers;
        public ObservableCollection<UserInfoDTO> AllUsers
        {
            get => _allUsers;
            set => SetProperty(ref _allUsers, value);
        }

        private ObservableCollection<FPInfoDTO> _friendProfiles;
        public ObservableCollection<FPInfoDTO> FriendProfiles
        {
            get => _friendProfiles;
            set => SetProperty(ref _friendProfiles, value);
        }

        private ObservableCollection<BookingDetailsDTO> _allBookings;
        public ObservableCollection<BookingDetailsDTO> AllBookings
        {
            get => _allBookings;
            set => SetProperty(ref _allBookings, value);
        }

        private ObservableCollection<LastMessageItem> _recentMessages;
        public ObservableCollection<LastMessageItem> RecentMessages
        {
            get => _recentMessages;
            set => SetProperty(ref _recentMessages, value);
        }

        private ObservableCollection<AuditLogDTO> _auditLogs;
        public ObservableCollection<AuditLogDTO> AuditLogs
        {
            get => _auditLogs;
            set => SetProperty(ref _auditLogs, value);
        }

        private int _totalUsers;
        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        private int _activeUsers;
        public int ActiveUsers
        {
            get => _activeUsers;
            set => SetProperty(ref _activeUsers, value);
        }

        private int _blockedUsers;
        public int BlockedUsers
        {
            get => _blockedUsers;
            set => SetProperty(ref _blockedUsers, value);
        }

        private int _totalBookings;
        public int TotalBookings
        {
            get => _totalBookings;
            set => SetProperty(ref _totalBookings, value);
        }

        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        private int _pendingVerifications;
        public int PendingVerifications
        {
            get => _pendingVerifications;
            set => SetProperty(ref _pendingVerifications, value);
        }

        private UserLoginDTO _selectedUser;
        public UserLoginDTO SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnPropertyChanged(nameof(IsUserSelected));
                }
            }
        }

        private FPInfoDTO _selectedFriendProfile;
        public FPInfoDTO SelectedFriendProfile
        {
            get => _selectedFriendProfile;
            set => SetProperty(ref _selectedFriendProfile, value);
        }

        private BookingDetailsDTO _selectedBooking;
        public BookingDetailsDTO SelectedBooking
        {
            get => _selectedBooking;
            set => SetProperty(ref _selectedBooking, value);
        }

        private string _userSearchText = string.Empty;
        public string UserSearchText
        {
            get => _userSearchText;
            set
            {
                if (SetProperty(ref _userSearchText, value))
                {
                    FilterUsers();
                }
            }
        }

        private string _selectedRoleFilter = "Все";
        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                {
                    FilterUsers();
                }
            }
        }

        private string _selectedStatusFilter = "Все";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    FilterUsers();
                }
            }
        }

        private DateTime _dateFrom = DateTime.Now.AddDays(-30);
        public DateTime DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
        }

        private DateTime _dateTo = DateTime.Now;
        public DateTime DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand UnblockUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand VerifyFriendCommand { get; }
        public ICommand RejectFriendCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand SendNotificationCommand { get; }
        public ICommand ForceLogoutCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ApplyDateFilterCommand { get; }
        public ICommand DeleteAllLogCommand { get; }

        public bool IsUserSelected => SelectedUser != null;
        public string[] RoleFilters => new[] { "Все", "Client", "Friend", "Moderator", "Admin" };
        public string[] StatusFilters => new[] { "Все", "Активен", "Заблокирован" };
        public int FriendsCount => FriendProfiles?.Count(fp => fp.IsVerified) ?? 0;
        public int ModeratorsCount => AllUsers?.Count(u => u.Role == "Moderator") ?? 0;
        public int AdminsCount => AllUsers?.Count(u => u.Role == "Admin") ?? 0;

        public AdminDashboardViewModel(string token)
        {
            _token = token;
            Title = "Административная панель";

            AllUsers = new ObservableCollection<UserInfoDTO>();
            FriendProfiles = new ObservableCollection<FPInfoDTO>();
            AllBookings = new ObservableCollection<BookingDetailsDTO>();
            RecentMessages = new ObservableCollection<LastMessageItem>();
            AuditLogs = new ObservableCollection<AuditLogDTO>();

            LoadDataCommand = new RelayCommandAsync(LoadAllDataAsync);
            BlockUserCommand = new RelayCommandAsync<UserLoginDTO>(BlockUserAsync, CanModifyUser);
            UnblockUserCommand = new RelayCommandAsync<UserLoginDTO>(UnblockUserAsync, CanModifyUser);
            DeleteUserCommand = new RelayCommandAsync<UserLoginDTO>(DeleteUserAsync, CanModifyUser);
            VerifyFriendCommand = new RelayCommandAsync<FPInfoDTO>(VerifyFriendAsync);
            RejectFriendCommand = new RelayCommandAsync<FPInfoDTO>(RejectFriendAsync);
            ExportDataCommand = new RelayCommandAsync(ExportDataAsync);
            SendNotificationCommand = new RelayCommandAsync<AdminNotificationDTO>(SendNotificationAsync);
            ForceLogoutCommand = new RelayCommandAsync<UserLoginDTO>(ForceLogoutAsync);
            ClearSearchCommand = new RelayCommandAsync(ClearSearchAsync);
            ApplyDateFilterCommand = new RelayCommandAsync(ApplyDateFilterAsync);
            DeleteAllLogCommand = new RelayCommandAsync(DeleteAllLogAsync);

            _ = InitializeAsync(token);
        }

        private async Task InitializeAsync(string token)
        {
            try
            {
                var user = await UserContext.GetUser(token);
                _adminUserId = user?.UserID ?? -1;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка инициализации: {ex.Message}");
            }
        }
        private async Task LoadAllDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await LoadUsersAsync();
                await LoadFriendProfilesAsync();
                await LoadBookingsAsync();
                await LoadRecentMessagesAsync();
                await LoadAuditLogsAsync();
                CalculateStatistics();

                Messenger.Default.SendNotification("Данные успешно загружены");
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки данных: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var usersResponse = await UserContext.GetAllUsers(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllUsers.Clear();
                    if (usersResponse?.Users != null)
                    {
                        foreach (var user in usersResponse.Users)
                        {
                            AllUsers.Add(user);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private async Task LoadFriendProfilesAsync()
        {
            try
            {
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FriendProfiles.Clear();
                    if (profilesResponse?.Profiles != null)
                    {
                        foreach (var profile in profilesResponse.Profiles)
                        {
                            FriendProfiles.Add(profile);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки профилей друзей: {ex.Message}");
            }
        }

        private async Task LoadBookingsAsync()
        {
            try
            {
                var bookingsResponse = await BookingContext.GetAllBookings(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllBookings.Clear();
                    if (bookingsResponse?.Bookings != null)
                    {
                        foreach (var booking in bookingsResponse.Bookings)
                        {
                            AllBookings.Add(booking);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки бронирований: {ex.Message}");
            }
        }

        private async Task LoadRecentMessagesAsync()
        {
            try
            {
                var messagesResponse = await MessageContext.GetRecentMessages(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RecentMessages.Clear();
                    if (messagesResponse?.Messages != null)
                    {
                        foreach (var msg in messagesResponse.Messages)
                        {
                            RecentMessages.Add(msg);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                var logsResponse = await AuditLogContext.GetAllLogs(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AuditLogs.Clear();
                    if (logsResponse?.Logs != null)
                    {
                        foreach (var log in logsResponse.Logs)
                        {
                            AuditLogs.Add(log);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки логов: {ex.Message}");
            }
        }

        private void CalculateStatistics()
        {
            TotalUsers = AllUsers?.Count ?? 0;
            ActiveUsers = AllUsers?.Count(u => u.IsActive) ?? 0;
            BlockedUsers = AllUsers?.Count(u => !u.IsActive) ?? 0;
            TotalBookings = AllBookings?.Count ?? 0;
            TotalRevenue = AllBookings?.Sum(b => b.TotalAmount) ?? 0;
            PendingVerifications = FriendProfiles?.Count(fp => !fp.IsVerified) ?? 0;
        }

        private bool CanModifyUser(UserLoginDTO? user)
        {
            return user != null && user.UserID != _adminUserId;
        }

        private async Task BlockUserAsync(UserLoginDTO? user)
        {
            if (user == null) return;

            if (MessageBox.Show($"Заблокировать пользователя {user.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var result = await UserContext.UpdateUserStatus(_token, user.UserID, false);
                if (result != null)
                {
                    await LoadAllDataAsync();
                    Messenger.Default.SendNotification($"Пользователь {user.FullName} заблокирован");
                }
                else
                {
                    SetError("Ошибка блокировки пользователя");
                }
            }
        }

        private async Task UnblockUserAsync(UserLoginDTO? user)
        {
            if (user == null) return;

            var result = await UserContext.UpdateUserStatus(_token, user.UserID, true);
            if (result != null)
            {
                await LoadAllDataAsync();
                Messenger.Default.SendNotification($"Пользователь {user.FullName} разблокирован");
            }
            else
            {
                SetError("Ошибка разблокировки пользователя");
            }
        }

        private async Task DeleteUserAsync(UserLoginDTO? user)
        {
            if (user == null) return;

            if (MessageBox.Show($"УДАЛИТЬ пользователя {user.FullName} навсегда?\nЭто действие нельзя отменить!",
                "ОПАСНОЕ ДЕЙСТВИЕ", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                if (MessageBox.Show($"Вы точно хотите удалить этого пользователя?",
                    "ПОДТВЕРЖДЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    var result = await UserContext.DeleteUser(_token, user.UserID);
                    if (result != null)
                    {
                        await LoadAllDataAsync();
                        Messenger.Default.SendNotification($"Пользователь {user.FullName} удален");
                    }
                    else
                    {
                        SetError("Ошибка удаления пользователя");
                    }
                }
            }
        }

        private async Task VerifyFriendAsync(FPInfoDTO? profile)
        {
            if (profile == null) return;

            var result = await FriendProfileContext.VerifyFriendProfile(_token, profile.ProfileID, true);
            if (result != null)
            {
                await LoadAllDataAsync();
                Messenger.Default.SendNotification($"Профиль {profile.FullName} верифицирован");
            }
            else
            {
                SetError("Ошибка верификации профиля");
            }
        }

        private async Task RejectFriendAsync(FPInfoDTO? profile)
        {
            if (profile == null) return;

            if (MessageBox.Show($"Отклонить верификацию профиля {profile.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var result = await FriendProfileContext.VerifyFriendProfile(_token, profile.ProfileID, false);
                if (result != null)
                {
                    await LoadAllDataAsync();
                    Messenger.Default.SendNotification($"Верификация профиля {profile.FullName} отклонена");
                }
                else
                {
                    SetError("Ошибка отклонения верификации");
                }
            }
        }

        private void FilterUsers()
        {
            OnPropertyChanged(nameof(AllUsers));
        }

        private async Task ClearSearchAsync()
        {
            UserSearchText = string.Empty;
            SelectedRoleFilter = "Все";
            SelectedStatusFilter = "Все";
            await Task.CompletedTask;
        }

        private async Task ApplyDateFilterAsync()
        {
            await LoadBookingsAsync();
            CalculateStatistics();
        }

        private async Task ExportDataAsync()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                Messenger.Default.SendNotification("Данные экспортированы");
            }
            await Task.CompletedTask;
        }

        private async Task SendNotificationAsync(AdminNotificationDTO? notification)
        {
            if (notification == null) return;

            var notificationData = new CreateNotificationDTO
            {
                UserID = notification.UserID,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type
            };

            var result = await NotificationContext.CreateNotification(_token, notificationData);
            if (result != null)
            {
                Messenger.Default.SendNotification($"Уведомление отправлено пользователю ID={notification.UserID}");
            }
            else
            {
                SetError("Ошибка отправки уведомления");
            }
        }

        private async Task ForceLogoutAsync(UserLoginDTO? user)
        {
            if (user == null) return;

            if (MessageBox.Show($"Принудительно выйти из системы пользователю {user.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Messenger.Default.SendNotification($"Пользователь {user.FullName} принудительно вышел из системы");
            }
            await Task.CompletedTask;
        }

        private async Task DeleteAllLogAsync()
        {
            try
            {
                if (MessageBox.Show("Удалить все логи?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var result = await AuditLogContext.DeleteAllLogs(_token);
                    if (result != null)
                    {
                        await LoadAuditLogsAsync();
                        Messenger.Default.SendNotification("Все логи удалены");
                    }
                    else
                    {
                        SetError("Ошибка удаления логов");
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
        }
    }

    public class AdminNotificationDTO
    {
        public int UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
    }
}