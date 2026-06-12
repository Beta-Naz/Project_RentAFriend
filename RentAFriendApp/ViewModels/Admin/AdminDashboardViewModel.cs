using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO.Response;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.ViewModels.Admin
{
    public class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private readonly string _token;
        private int _adminUserId;

        private ObservableCollection<UserInfoItem> _allUsersFull = new();
        private ObservableCollection<UserInfoItem> _allUsers = new();
        public ObservableCollection<UserInfoItem> AllUsers
        {
            get => _allUsers;
            set { _allUsers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<FPInfoDTO> _friendProfiles = new();
        public ObservableCollection<FPInfoDTO> FriendProfiles
        {
            get => _friendProfiles;
            set { _friendProfiles = value; OnPropertyChanged(); }
        }

        private ObservableCollection<BookingDetailsDTO> _allBookings = new();
        public ObservableCollection<BookingDetailsDTO> AllBookings
        {
            get => _allBookings;
            set { _allBookings = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LastMessageItem> _recentMessages = new();
        public ObservableCollection<LastMessageItem> RecentMessages
        {
            get => _recentMessages;
            set { _recentMessages = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AuditLogItem> _auditLogs = new();
        public ObservableCollection<AuditLogItem> AuditLogs
        {
            get => _auditLogs;
            set { _auditLogs = value; OnPropertyChanged(); }
        }

        // ===== СТАТИСТИКА =====
        private int _totalUsers;
        public int TotalUsers { get => _totalUsers; set { _totalUsers = value; OnPropertyChanged(); } }

        private int _activeUsers;
        public int ActiveUsers { get => _activeUsers; set { _activeUsers = value; OnPropertyChanged(); } }

        private int _blockedUsers;
        public int BlockedUsers { get => _blockedUsers; set { _blockedUsers = value; OnPropertyChanged(); } }

        private int _totalBookings;
        public int TotalBookings { get => _totalBookings; set { _totalBookings = value; OnPropertyChanged(); } }

        private decimal _totalRevenue;
        public decimal TotalRevenue { get => _totalRevenue; set { _totalRevenue = value; OnPropertyChanged(); } }

        private int _pendingVerifications;
        public int PendingVerifications { get => _pendingVerifications; set { _pendingVerifications = value; OnPropertyChanged(); } }

        private int _onlineUsers = 0;
        public int OnlineUsers { get => _onlineUsers; set { _onlineUsers = value; OnPropertyChanged(); } }

        private string _userSearchText = "";
        public string UserSearchText
        {
            get => _userSearchText;
            set { _userSearchText = value; OnPropertyChanged(); FilterUsers(); }
        }

        private string _selectedRoleFilter = "Все";
        public string SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set { _selectedRoleFilter = value; OnPropertyChanged(); FilterUsers(); }
        }

        private string _selectedStatusFilter = "Все";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); FilterUsers(); }
        }

        private DateTime _dateFrom = DateTime.Now.AddDays(-30);
        public DateTime DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); }
        }

        private DateTime _dateTo = DateTime.Now;
        public DateTime DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); }
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        // ===== РАССЫЛКА =====
        private string _broadcastTitle = "";
        public string BroadcastTitle
        {
            get => _broadcastTitle;
            set { _broadcastTitle = value; OnPropertyChanged(); }
        }

        private string _broadcastMessage = "";
        public string BroadcastMessage
        {
            get => _broadcastMessage;
            set { _broadcastMessage = value; OnPropertyChanged(); }
        }

        // ===== СОСТОЯНИЕ =====
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string[] RoleFilters => new[] { "Все", "Client", "Friend", "Admin" };
        public string[] StatusFilters => new[] { "Все", "Активен", "Заблокирован" };

        // ===== КОМАНДЫ =====
        public ICommand LoadDataCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand UnblockUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand VerifyFriendCommand { get; }
        public ICommand RejectFriendCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ApplyDateFilterCommand { get; }
        public ICommand DeleteAllLogCommand { get; }
        public ICommand SendBroadcastCommand { get; }

        public AdminDashboardViewModel(string token)
        {
            _token = token;

            LoadDataCommand = new RelayCommand(async () => await LoadAllDataAsync());
            BlockUserCommand = new RelayCommand<UserInfoItem>(async (u) => await BlockUserAsync(u), CanModifyUser);
            UnblockUserCommand = new RelayCommand<UserInfoItem>(async (u) => await UnblockUserAsync(u), CanModifyUser);
            DeleteUserCommand = new RelayCommand<UserInfoItem>(async (u) => await DeleteUserAsync(u), CanModifyUser);
            VerifyFriendCommand = new RelayCommand<FPInfoDTO>(async (p) => await VerifyFriendAsync(p));
            RejectFriendCommand = new RelayCommand<FPInfoDTO>(async (p) => await RejectFriendAsync(p));
            ExportDataCommand = new RelayCommand(async () => await ExportDataAsync());
            ClearSearchCommand = new RelayCommand(() => ClearSearch());
            ApplyDateFilterCommand = new RelayCommand(async () => await LoadBookingsAsync());
            DeleteAllLogCommand = new RelayCommand(async () => await DeleteAllLogsAsync());
            SendBroadcastCommand = new RelayCommand(async () => await SendBroadcastAsync());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var user = await UserContext.GetUser(_token);
                _adminUserId = user?.Data?.UserID ?? -1;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка инициализации: {ex.Message}");
            }
        }

        public async Task LoadAllDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                await Task.WhenAll(
                    LoadUsersAsync(),
                    LoadFriendProfilesAsync(),
                    LoadBookingsAsync(),
                    LoadMessagesAsync(),
                    LoadAuditLogsAsync()
                );

                CalculateStatistics();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadUsersAsync()
        {
            var response = await UserContext.GetAllUsers(_token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _allUsersFull.Clear();
                if (response?.Users != null)
                    foreach (var u in response.Users)
                        _allUsersFull.Add(u);
                FilterUsers();
            });
        }

        private async Task LoadFriendProfilesAsync()
        {
            var response = await FriendProfileContext.GetAllProfiles(_token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                FriendProfiles.Clear();
                if (response?.Profiles != null)
                    foreach (var p in response.Profiles)
                        FriendProfiles.Add(p);
            });
        }

        private async Task LoadBookingsAsync()
        {
            var response = await BookingContext.GetAllBookings(_token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AllBookings.Clear();
                if (response?.Bookings != null)
                    foreach (var b in response.Bookings)
                        AllBookings.Add(b);
            });
        }

        private async Task LoadMessagesAsync()
        {
            var response = await MessageContext.GetRecentMessages(_token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RecentMessages.Clear();
                if (response?.Messages != null)
                    foreach (var m in response.Messages)
                        RecentMessages.Add(m);
            });
        }

        private async Task LoadAuditLogsAsync()
        {
            var response = await AuditLogContext.GetAllLogs(_token);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AuditLogs.Clear();
                if (response?.Logs != null)
                    foreach (var log in response.Logs)
                        AuditLogs.Add(new AuditLogItem
                        {
                            LogID = log.LogID,
                            UserID = log.UserID ?? 0,
                            UserName = log.UserName ?? "Система",
                            Action = log.Action,
                            TableName = log.TableName,
                            RecordID = log.RecordID,
                            OldValue = log.OldValue ?? "",
                            NewValue = log.NewValue ?? "",
                            LoggedAt = log.LoggedAt,
                            ActionColor = GetActionColor(log.Action)
                        });
            });
        }

        private void CalculateStatistics()
        {
            TotalUsers = _allUsersFull.Count;
            ActiveUsers = _allUsersFull.Count(u => u.IsActive);
            BlockedUsers = _allUsersFull.Count(u => !u.IsActive);
            TotalBookings = AllBookings.Count;
            TotalRevenue = AllBookings.Sum(b => b.TotalAmount);
            PendingVerifications = FriendProfiles.Count(p => !p.IsVerified);
            OnlineUsers = new Random().Next(5, 50); // заглушка
        }

        private void FilterUsers()
        {
            var filtered = _allUsersFull.Where(u =>
                (SelectedRoleFilter == "Все" || u.Role == SelectedRoleFilter) &&
                (SelectedStatusFilter == "Все" ||
                 (SelectedStatusFilter == "Активен" && u.IsActive) ||
                 (SelectedStatusFilter == "Заблокирован" && !u.IsActive)) &&
                (string.IsNullOrWhiteSpace(UserSearchText) ||
                 (u.FullName?.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (u.Email?.Contains(UserSearchText, StringComparison.OrdinalIgnoreCase) ?? false))
            ).ToList();

            AllUsers = new ObservableCollection<UserInfoItem>(filtered);
        }

        private void ClearSearch()
        {
            UserSearchText = "";
            SelectedRoleFilter = "Все";
            SelectedStatusFilter = "Все";
        }

        private bool CanModifyUser(UserInfoItem? user) => user != null && user.UserID != _adminUserId;

        private async Task BlockUserAsync(UserInfoItem? user)
        {
            if (user == null) return;
            if (MessageBox.Show($"Заблокировать {user.FullName}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var result = await UserContext.UpdateUserStatus(_token, user.UserID, false);
            if (result?.Ok == true)
            {
                user.IsActive = false;
                CalculateStatistics();
            }
            else SetError("Ошибка блокировки");
        }

        private async Task UnblockUserAsync(UserInfoItem? user)
        {
            if (user == null) return;
            var result = await UserContext.UpdateUserStatus(_token, user.UserID, true);
            if (result?.Ok == true)
            {
                user.IsActive = true;
                CalculateStatistics();
            }
            else SetError("Ошибка разблокировки");
        }

        private async Task DeleteUserAsync(UserInfoItem? user)
        {
            if (user == null) return;
            if (MessageBox.Show($"УДАЛИТЬ {user.FullName} навсегда?", "Опасно!",
                MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Точно?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes) return;

            var result = await UserContext.DeleteUser(_token, user.UserID);
            if (result != null)
                await LoadAllDataAsync();
            else
                SetError("Ошибка удаления");
        }

        private async Task VerifyFriendAsync(FPInfoDTO? profile)
        {
            if (profile == null) return;
            var result = await FriendProfileContext.VerifyFriendProfile(_token, profile.ProfileID, true);
            if (result != null)
                await LoadAllDataAsync();
            else
                SetError("Ошибка верификации");
        }

        private async Task RejectFriendAsync(FPInfoDTO? profile)
        {
            if (profile == null) return;

            // Диалог с причиной отклонения
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Укажите причину отклонения:", "Отклонение верификации", "");

            if (string.IsNullOrWhiteSpace(reason)) return;

            var result = await FriendProfileContext.VerifyFriendProfile(_token, profile.ProfileID, false);
            if (result != null)
            {
                // Отправляем уведомление другу о причине отклонения
                await NotificationContext.CreateNotification(_token, new RentAFriendApp.Models.ClassesDTO.NotificationDTO.CreateNotificationDTO
                {
                    UserID = profile.UserID,
                    Title = "Верификация отклонена",
                    Message = $"Ваша верификация отклонена. Причина: {reason}",
                    Type = "Verification"
                });
                await LoadAllDataAsync();
            }
            else
                SetError("Ошибка отклонения");
        }

        private async Task SendBroadcastAsync()
        {
            if (string.IsNullOrWhiteSpace(BroadcastTitle) || string.IsNullOrWhiteSpace(BroadcastMessage))
            {
                SetError("Заполните заголовок и текст сообщения");
                return;
            }

            if (MessageBox.Show($"Отправить сообщение ВСЕМ пользователям?", "Рассылка",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                IsBusy = true;
                int sent = 0;
                foreach (var user in _allUsersFull)
                {
                    await NotificationContext.CreateNotification(_token, new RentAFriendApp.Models.ClassesDTO.NotificationDTO.CreateNotificationDTO
                    {
                        UserID = user.UserID,
                        Title = BroadcastTitle,
                        Message = BroadcastMessage,
                        Type = "System"
                    });
                    sent++;
                }

                BroadcastTitle = "";
                BroadcastMessage = "";
                MessageBox.Show($"Отправлено {sent} уведомлений", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetError($"Ошибка рассылки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportDataAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"RentAFriend_Export_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                IsBusy = true;
                var exportService = new Services.ExcelExportService(_token);

                bool success = false;

                switch (SelectedTabIndex)
                {
                    case 0:
                        success = await exportService.ExportUsersAsync(dialog.FileName, _allUsersFull.ToList());
                        break;
                    case 4: 
                        success = await exportService.ExportLogsAsync(dialog.FileName);
                        break;
                    default:
                        success = await exportService.ExportStatisticsAsync(dialog.FileName);
                        break;
                }

                if (success)
                    MessageBox.Show($"Данные экспортированы в:\n{dialog.FileName}", "Экспорт успешен",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Не удалось экспортировать данные", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteAllLogsAsync()
        {
            if (MessageBox.Show("Удалить ВСЕ логи аудита?", "Опасно!",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var result = await AuditLogContext.DeleteAllLogs(_token);
            if (result != null)
            {
                AuditLogs.Clear();
                MessageBox.Show("Логи удалены", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                SetError("Ошибка удаления логов");
        }

        public void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        public void ClearErrors()
        {
            ErrorMessage = "";
            HasError = false;
        }

        private SolidColorBrush GetActionColor(string action)
        {
            var color = action?.ToUpper() switch
            {
                string s when s.Contains("DELETE") || s.Contains("BLOCK") => Color.FromRgb(244, 67, 54),
                string s when s.Contains("CREATE") || s.Contains("VERIFY") || s.Contains("UNBLOCK") => Color.FromRgb(76, 175, 80),
                string s when s.Contains("UPDATE") => Color.FromRgb(255, 152, 0),
                string s when s.Contains("LOGIN") => Color.FromRgb(33, 150, 243),
                string s when s.Contains("LOGOUT") => Color.FromRgb(158, 158, 158),
                _ => Color.FromRgb(158, 158, 158)
            };
            return new SolidColorBrush(color);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AuditLogItem
    {
        public int LogID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string TableName { get; set; } = "";
        public int RecordID { get; set; }
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public DateTime LoggedAt { get; set; }
        public SolidColorBrush ActionColor { get; set; } = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}