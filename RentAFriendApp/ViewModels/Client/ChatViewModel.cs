using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ChatDTO;
using RentAFriendApp.Models.ClassesDTO.ChatDTO.Response;
using RentAFriendApp.Models.ClassesDTO.MessageDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO.Response;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class ChatViewModel : BaseViewModel
    {
        private readonly string _token;

        private ChatListDTO _selectedChat;
        public ChatListDTO SelectedChat
        {
            get => _selectedChat;
            set
            {
                if (SetProperty(ref _selectedChat, value) && value != null)
                {
                    _ = LoadMessagesAsync(value.ChatID);
                }
            }
        }

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set
            {
                SetProperty(ref _messageText, value);
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                SetProperty(ref _searchQuery, value);
                FilterChats();
            }
        }

        private ObservableCollection<ChatListDTO> _allChats = new();
        public ObservableCollection<ChatListDTO> AllChats
        {
            get => _allChats;
            set => SetProperty(ref _allChats, value);
        }

        private ObservableCollection<ChatListDTO> _filteredChats = new();
        public ObservableCollection<ChatListDTO> FilteredChats
        {
            get => _filteredChats;
            set => SetProperty(ref _filteredChats, value);
        }

        private ObservableCollection<MessageDTO> _messages = new();
        public ObservableCollection<MessageDTO> Messages
        {
            get => _messages;
            set => SetProperty(ref _messages, value);
        }

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && SelectedChat != null;

        // Команды
        public ICommand SelectChatCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand RefreshCommand { get; }

        public ChatViewModel(string token)
        {
            _token = token;

            SelectChatCommand = new RelayCommandAsync<ChatListDTO>(SelectChatAsync);
            SendMessageCommand = new RelayCommandAsync(SendMessageAsync, () => CanSendMessage);
            SearchCommand = new RelayCommandAsync(FilterChatsAsync);
            ClearSearchCommand = new RelayCommandAsync(() => { SearchQuery = ""; return Task.CompletedTask; });
            RefreshCommand = new RelayCommandAsync(LoadChatsAsync);

            // Загрузка чатов при инициализации
            _ = LoadChatsAsync();
        }

        private async Task LoadChatsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var chatsResponse = await ChatContext.GetMyChats(_token, page: 1, pageSize: 50);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var newAllChats = new ObservableCollection<ChatListDTO>();

                    if (chatsResponse?.Chats != null)
                    {
                        foreach (var chat in chatsResponse.Chats)
                        {
                            newAllChats.Add(chat);
                        }
                    }

                    AllChats = newAllChats;
                    FilterChats();

                    // Если есть чаты, выбираем первый
                    if (FilteredChats.Count > 0)
                    {
                        SelectedChat = FilteredChats[0];
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки чатов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SelectChatAsync(ChatListDTO chat)
        {
            if (chat == null) return;

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedChat = chat;
                });

                await LoadMessagesAsync(chat.ChatID);

                if (chat.UnreadCount > 0)
                {
                    await MessageContext.MarkMessagesAsRead(_token, chat.ChatID);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        chat.UnreadCount = 0;

                        var updatedChats = AllChats.ToList();
                        var index = updatedChats.FindIndex(c => c.ChatID == chat.ChatID);
                        if (index >= 0)
                        {
                            updatedChats[index] = chat;
                            AllChats = new ObservableCollection<ChatListDTO>(updatedChats);
                            FilterChats();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критический баг! Код ошибки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMessagesAsync(int chatId)
        {
            try
            {
                IsBusy = true;

                var messagesResponse = await MessageContext.GetMessages(_token, chatId, page: 1, pageSize: 100);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var newMessages = new ObservableCollection<MessageDTO>();

                    if (messagesResponse?.Messages != null)
                    {
                        foreach (var msg in messagesResponse.Messages)
                        {
                            newMessages.Add(msg);
                        }
                    }

                    Messages = newMessages;
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки сообщений: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage || SelectedChat == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var sendRequest = new SendMessageDTO
                {
                    ChatID = SelectedChat.ChatID,
                    Content = MessageText,
                    MessageType = "Text"
                };

                var result = await MessageContext.SendMessage(_token, sendRequest);

                if (result != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        // Создаем новое сообщение для отображения
                        var newMessage = new MessageDTO
                        {
                            MessageID = result.MessageId,
                            SenderID = 0, // Заполнится при перезагрузке
                            Content = MessageText,
                            MessageType = "Text",
                            IsRead = false,
                            IsEdited = false,
                            CreatedAt = result.SentAt
                        };

                        var newMessages = new ObservableCollection<MessageDTO>(Messages);
                        newMessages.Add(newMessage);
                        Messages = newMessages;

                        // Обновляем информацию о чате
                        var chat = AllChats.FirstOrDefault(c => c.ChatID == SelectedChat.ChatID);
                        if (chat != null)
                        {
                            chat.LastMessage = MessageText.Length > 30
                                ? MessageText.Substring(0, 30) + "..."
                                : MessageText;
                            chat.LastMessageAt = result.SentAt;

                            // Обновляем список чатов
                            var updatedChats = AllChats.ToList();
                            var index = updatedChats.FindIndex(c => c.ChatID == chat.ChatID);
                            if (index >= 0)
                            {
                                updatedChats[index] = chat;
                                AllChats = new ObservableCollection<ChatListDTO>(updatedChats);
                                FilterChats();
                            }
                        }

                        // Очищаем поле ввода
                        MessageText = string.Empty;

                        // Перезагружаем сообщения для обновления статуса
                        await LoadMessagesAsync(SelectedChat.ChatID);
                    });
                }
                else
                {
                    SetError("Ошибка отправки сообщения");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка отправки сообщения: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterChats()
        {
            _ = FilterChatsAsync();
        }

        private async Task FilterChatsAsync()
        {
            await Task.Run(() =>
            {
                var filtered = AllChats.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var query = SearchQuery.ToLower();
                    filtered = filtered.Where(c =>
                        c.InterlocutorName?.ToLower().Contains(query) == true ||
                        (c.LastMessage?.ToLower().Contains(query) == true)
                    );
                }

                var filteredList = filtered.ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredChats = new ObservableCollection<ChatListDTO>(filteredList);
                });
            });
        }
    }
}