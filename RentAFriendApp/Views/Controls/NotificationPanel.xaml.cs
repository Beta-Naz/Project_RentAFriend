using RentAFriendApp.Context;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentAFriendApp.Views.Controls
{
    /// <summary>
    /// Логика взаимодействия для NotificationPanel.xaml
    /// </summary>
    public partial class NotificationPanel : UserControl
    {
        private string _token;
        private bool _isUnreadTab = true;
        private List<NotificationItem> _unreadItems = new();
        private List<NotificationItem> _historyItems = new();

        public event Action? OnCloseRequested;
        public event Action<int>? OnUnreadCountChanged;

        public NotificationPanel(string token)
        {
            InitializeComponent();
            _token = token;
            Loaded += async (s, e) => await LoadNotifications();
        }

        private async Task LoadNotifications()
        {
            try
            {
                var response = await NotificationContext.GetMyNotifications(_token, page: 1, pageSize: 50, onlyUnread: false);
                if (response?.Notifications == null) return;

                _unreadItems.Clear();
                _historyItems.Clear();

                foreach (var n in response.Notifications)
                {
                    var item = new NotificationItem
                    {
                        Id = n.NotificationID,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    };

                    if (n.IsRead)
                        _historyItems.Add(item);
                    else
                        _unreadItems.Add(item);
                }

                RenderNotifications();
            }
            catch { /* тихо */ }
        }

        private void RenderNotifications()
        {
            NotificationsStackPanel.Children.Clear();
            var items = _isUnreadTab ? _unreadItems : _historyItems;

            if (items.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = _isUnreadTab ? "Нет новых уведомлений" : "История пуста",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 40)
                };
                NotificationsStackPanel.Children.Add(emptyText);
                return;
            }

            foreach (var item in items)
            {
                NotificationsStackPanel.Children.Add(CreateNotificationCard(item));
            }
        }

        private Border CreateNotificationCard(NotificationItem item)
        {
            var isUnread = !item.IsRead;
            var bgColor = isUnread ? Color.FromRgb(0xF1, 0xF8, 0xE9) : Colors.White;
            var textWeight = isUnread ? FontWeights.SemiBold : FontWeights.Normal;

            // Карточка
            var card = new Border
            {
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 10, 10, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = item
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (isUnread)
            {
                var indicator = new Border
                {
                    Width = 4,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(0, 2, 10, 2)
                };
                Grid.SetColumn(indicator, 0);
                grid.Children.Add(indicator);
            }

            var contentStack = new StackPanel { Margin = new Thickness(isUnread ? 0 : 0, 0, 8, 0) };
            Grid.SetColumn(contentStack, 1);

            var titleBlock = new TextBlock
            {
                Text = item.Title,
                FontWeight = textWeight,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var messageBlock = new TextBlock
            {
                Text = item.Message,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 34,
                Margin = new Thickness(0, 2, 0, 0)
            };
            var timeBlock = new TextBlock
            {
                Text = FormatTime(item.CreatedAt),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                Margin = new Thickness(0, 4, 0, 0)
            };

            contentStack.Children.Add(titleBlock);
            contentStack.Children.Add(messageBlock);
            contentStack.Children.Add(timeBlock);
            grid.Children.Add(contentStack);

            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0
            };
            Grid.SetColumn(actionsPanel, 2);

            if (isUnread)
            {
                var markReadBtn = new Button
                {
                    Content = "✓",
                    Style = FindResource("ActionButtonStyle") as Style,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    FontSize = 14,
                    ToolTip = "Отметить прочитанным"
                };
                markReadBtn.Click += async (s, e) => await MarkAsRead(item);
                actionsPanel.Children.Add(markReadBtn);
            }

            var deleteBtn = new Button
            {
                Content = "🗑",
                Style = FindResource("ActionButtonStyle") as Style,
                Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                FontSize = 13,
                ToolTip = "Удалить"
            };
            deleteBtn.Click += async (s, e) => await DeleteNotification(item);
            actionsPanel.Children.Add(deleteBtn);

            grid.Children.Add(actionsPanel);

            card.MouseEnter += (s, e) => actionsPanel.Opacity = 1;
            card.MouseLeave += (s, e) => actionsPanel.Opacity = 0;

            card.Child = grid;
            return card;
        }

        private async Task MarkAsRead(NotificationItem item)
        {
            try
            {
                await NotificationContext.MarkAsRead(_token, item.Id);
                item.IsRead = true;
                _unreadItems.Remove(item);
                _historyItems.Insert(0, item);
                RenderNotifications();
                OnUnreadCountChanged?.Invoke(_unreadItems.Count);
            }
            catch { }
        }

        private async Task DeleteNotification(NotificationItem item)
        {
            try
            {
                await NotificationContext.DeleteNotification(_token, item.Id);
                _unreadItems.Remove(item);
                _historyItems.Remove(item);
                RenderNotifications();
                if (!item.IsRead)
                    OnUnreadCountChanged?.Invoke(_unreadItems.Count);
            }
            catch { }
        }

        private async void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_unreadItems.Count == 0) return;
                await NotificationContext.MarkAllAsRead(_token);
                _historyItems.InsertRange(0, _unreadItems);
                _unreadItems.Clear();
                RenderNotifications();
                OnUnreadCountChanged?.Invoke(0);
            }
            catch { }
        }

        private async void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            var items = _isUnreadTab ? _unreadItems : _historyItems;
            if (items.Count == 0) return;

            var result = MessageBox.Show(
                _isUnreadTab ? "Удалить все непрочитанные уведомления?" : "Очистить всю историю?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await NotificationContext.DeleteAllNotifications(_token);
                if (_isUnreadTab)
                    _unreadItems.Clear();
                else
                    _historyItems.Clear();
                RenderNotifications();
                OnUnreadCountChanged?.Invoke(_unreadItems.Count);
            }
            catch { }
        }

        private void BtnUnreadTab_Click(object sender, RoutedEventArgs e)
        {
            _isUnreadTab = true;
            BtnUnreadTab.Style = FindResource("ActiveTabButtonStyle") as Style;
            BtnHistoryTab.Style = FindResource("TabButtonStyle") as Style;
            RenderNotifications();
        }

        private void BtnHistoryTab_Click(object sender, RoutedEventArgs e)
        {
            _isUnreadTab = false;
            BtnHistoryTab.Style = FindResource("ActiveTabButtonStyle") as Style;
            BtnUnreadTab.Style = FindResource("TabButtonStyle") as Style;
            RenderNotifications();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            OnCloseRequested?.Invoke();
        }

        private string FormatTime(DateTime dt)
        {
            var local = dt.ToLocalTime();
            var diff = DateTime.Now - local;

            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{Math.Floor(diff.TotalMinutes)} мин. назад";
            if (diff.TotalHours < 24) return $"{Math.Floor(diff.TotalHours)} ч. назад";
            if (diff.TotalDays < 7) return $"{Math.Floor(diff.TotalDays)} дн. назад";
            return local.ToString("dd.MM.yyyy");
        }

        public int UnreadCount => _unreadItems.Count;
    }

    public class NotificationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
