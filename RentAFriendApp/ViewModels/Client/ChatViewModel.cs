using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ChatDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO;
using RentAFriendApp.ViewModels.Base;

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
                    LoadMessagesAsync(value.ChatID);
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
            LoadChatsAsync();
        }

        private async Task LoadChatsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var chatsResponse = await ChatContext.GetMyChats(_token, page: 1, pageSize: 50);

                // Работаем с коллекцией в UI потоке безопасно
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllChats.Clear();

                    if (chatsResponse?.Chats != null)
                    {
                        foreach (var chat in chatsResponse.Chats)
                        {
                            AllChats.Add(chat);
                        }
                    }

                    FilterChats(); // Обновляем отфильтрованный список

                    // Автовыбор только если ничего не выбрано и есть чаты
                    if (SelectedChat == null && FilteredChats.Count > 0)
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

            if (SelectedChat?.ChatID == chat.ChatID) return;

            SelectedChat = chat;

            if (chat.UnreadCount > 0)
            {
                chat.UnreadCount = 0;
                _ = MessageContext.MarkMessagesAsRead(_token, chat.ChatID);
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
                    Messages.Clear();
                    if (messagesResponse?.Messages != null)
                    {
                        foreach (var msg in messagesResponse.Messages)
                        {
                            Messages.Add(msg);
                        }
                    }
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

            string contentToSend = MessageText; // Сохраняем текст до очистки

            try
            {
                IsBusy = true;
                ClearErrors();

                var sendRequest = new SendMessageDTO
                {
                    ChatID = SelectedChat.ChatID,
                    Content = contentToSend,
                    MessageType = "Text"
                };

                var result = await MessageContext.SendMessage(_token, sendRequest);

                if (result != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageText = string.Empty;

                        var localMsg = new MessageDTO
                        {
                            MessageID = result.MessageId,
                            SenderID = 0,
                            Content = contentToSend,
                            MessageType = "Text",
                            IsRead = false,
                            CreatedAt = result.SentAt
                        };
                        Messages.Add(localMsg);


                        var chatInList = AllChats.FirstOrDefault(c => c.ChatID == SelectedChat.ChatID);
                        if (chatInList != null)
                        {
                            chatInList.LastMessage = contentToSend.Length > 30
                                ? contentToSend.Substring(0, 30) + "..."
                                : contentToSend;
                            chatInList.LastMessageAt = result.SentAt;


                            AllChats.Remove(chatInList);
                            AllChats.Insert(0, chatInList);

                            if (!string.IsNullOrWhiteSpace(SearchQuery))
                            {
                                FilterChats();
                            }
                        }
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
            var query = SearchQuery?.ToLower().Trim();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? AllChats.ToList()
                : AllChats.Where(c =>
                    (c.InterlocutorName != null && c.InterlocutorName.ToLower().Contains(query)) ||
                    (c.LastMessage != null && c.LastMessage.ToLower().Contains(query))
                  ).ToList();

            FilteredChats.Clear();
            foreach (var chat in filtered)
            {
                FilteredChats.Add(chat);
            }
        }

        private Task FilterChatsAsync()
        {
            FilterChats();
            return Task.CompletedTask;
        }
    }
}