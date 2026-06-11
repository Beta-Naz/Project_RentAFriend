using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ChatDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO;
using RentAFriendApp.ViewModels.Client;

namespace RentAFriendApp.Views.Client
{
    public partial class ChatPage : Page
    {
        private readonly string _token;
        private readonly ChatViewModel _viewModel;
        private readonly int _targetFriendId;
        private bool _isInitialized = false;
        private bool _isActive = false;

        public ChatPage(string token, int friendId = -1)
        {
            InitializeComponent();
            _isActive = true;
            _token = token;
            _targetFriendId = friendId;

            _viewModel = new ChatViewModel(_token);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatViewModel.Messages))
            {
                bool hasMessages = _viewModel.Messages != null && _viewModel.Messages.Any();
                noMessages.Visibility = hasMessages ? Visibility.Collapsed : Visibility.Visible;

                ScrollToLastMessage();
            }

            if (e.PropertyName == nameof(ChatViewModel.FilteredChats))
            {
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (_targetFriendId > 0)
            {
                OpenOrCreateChatWithFriend();
            }
            else
            {
                SearchTextBox.Focus();
            }

            _viewModel.RefreshCommand.Execute(null);
        }

        private async void OpenOrCreateChatWithFriend()
        {
            try
            {
                _viewModel.IsBusy = true;

                // Получаем или создаем чат через контекст
                var chat = await ChatContext.GetOrCreateChat(_token, _targetFriendId);

                if (chat != null)
                {
                    // Создаем объект чата для ViewModel
                    var chatListDto = new ChatListDTO
                    {
                        ChatID = chat.ChatId,
                        InterlocutorID = _targetFriendId,
                        InterlocutorName = chat.Interlocutor.Name,
                        CreatedAt = chat.CreatedAt,
                        IsActive = chat.IsActive
                    };

                    _viewModel.SelectChatCommand.Execute(chatListDto);
                    MessageTextBox.Focus();
                    ChatPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Не удалось создать чат.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии чата: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsBusy = false;
            }
        }

        // ——————— Навигация ———————
        private void BackToChatsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        // ——————— Поиск ———————
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск чатов...")
            {
                SearchTextBox.Text = string.Empty;
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск чатов...";
                SearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
                _viewModel.SearchQuery = string.Empty;
            }
        }


        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isActive) return;
            if (SearchTextBox.Text == "Поиск чатов...")
            {
                _viewModel.SearchQuery = string.Empty;
            }
            else
            {
                _viewModel.SearchQuery = SearchTextBox.Text;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _viewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "Поиск чатов...";
            SearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            _viewModel.ClearSearchCommand.Execute(null);
        }

        // ——————— Чаты ———————
        private void ChatList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ChatPanel.Visibility = Visibility.Visible;
        }

        // ——————— Контекстное меню ———————
        private void ChatList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!(ChatList.SelectedItem is ChatListDTO selectedChat))
            {
                e.Handled = true;
                return;
            }

            var contextMenu = new ContextMenu();

            var markReadItem = new MenuItem
            {
                Header = "Отметить как прочитанные",
                Tag = selectedChat.ChatID
            };
            markReadItem.Click += async (s, args) =>
            {
                await MessageContext.MarkMessagesAsRead(_token, selectedChat.ChatID);
                _viewModel.RefreshCommand.Execute(null);
            };

            var deleteItem = new MenuItem
            {
                Header = "Удалить чат",
                Tag = selectedChat.ChatID
            };
            deleteItem.Click += async (s, args) =>
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить этот чат?\nВсе сообщения будут удалены.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Закрываем чат (soft-delete)
                    var closeResult = await ChatContext.CloseChat(_token, selectedChat.ChatID);
                    if (closeResult != null)
                    {
                        _viewModel.RefreshCommand.Execute(null);
                        MessageBox.Show("Чат закрыт.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            };

            contextMenu.Items.Add(markReadItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteItem);

            ChatList.ContextMenu = contextMenu;
        }

        // ——————— Отправка сообщений ———————
        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (MessageTextBox.Text == "Напишите сообщение...")
            {
                MessageTextBox.Text = string.Empty;
                MessageTextBox.Foreground = Brushes.Black;
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageTextBox.Text = "Напишите сообщение...";
                MessageTextBox.Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    var tb = (TextBox)sender;
                    int caret = tb.CaretIndex;
                    tb.Text = tb.Text.Insert(caret, Environment.NewLine);
                    tb.CaretIndex = caret + Environment.NewLine.Length;
                    e.Handled = true;
                }
                else
                {
                    SendMessage();
                    e.Handled = true;
                }
            }
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageTextBox.Text != "Напишите сообщение...")
            {
                noMessages.Visibility = Visibility.Collapsed;
                SendMessage();
            }
        }

        private async void SendMessage()
        {
            if (!_viewModel.CanSendMessage || _viewModel.SelectedChat == null) return;

            string content = _viewModel.MessageText?.Trim();
            if (string.IsNullOrEmpty(content)) return;

            try
            {
                _viewModel.SendMessageCommand.Execute(null);
                MessageTextBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось отправить сообщение: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ——————— Файлы ———————
        private async void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Прикрепить файл",
                Filter = "Все файлы (*.*)|*.*|" +
                         "Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|" +
                         "Документы (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt"
            };

            if (dialog.ShowDialog() != true) return;

            string fileName = System.IO.Path.GetFileName(dialog.FileName);
            long fileSize = new System.IO.FileInfo(dialog.FileName).Length;

            if (fileSize > 10 * 1024 * 1024)
            {
                MessageBox.Show("Файл слишком большой. Максимум — 10 МБ.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _viewModel.IsBusy = true;

                // TODO: Загрузка файла на сервер
                // Пока отправляем только имя файла как текстовое сообщение
                var sendRequest = new SendMessageDTO
                {
                    ChatID = _viewModel.SelectedChat.ChatID,
                    Content = $"📎 Файл: {fileName}",
                    MessageType = "Text"
                };

                await MessageContext.SendMessage(_token, sendRequest);
                _viewModel.RefreshCommand.Execute(null);

                MessageBox.Show($"Файл '{fileName}' отправлен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _viewModel.IsBusy = false;
            }
        }

        // ——————— Обновление и прокрутка ———————
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshCommand.Execute(null);
        }

        private void ScrollToLastMessage()
        {
            if (MessagesScrollViewer == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagesScrollViewer.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // ——————— Звонки ———————
        private void CallButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Голосовые звонки будут доступны в следующем обновлении.", "В разработке",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void VideoCallButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Видеозвонки будут доступны в следующем обновлении.", "В разработке",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ——————— Заглушки ———————
        private void MenuButton_Click(object sender, RoutedEventArgs e) { }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Выбор эмодзи будет реализован позже.", "В разработке",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChatList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatList.SelectedItem is ChatListDTO selectedChat)
            {
                if (_viewModel.SelectedChat?.ChatID != selectedChat.ChatID)
                {
                    _viewModel.SelectChatCommand.Execute(selectedChat);
                    ChatPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _isActive = false;
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }
}