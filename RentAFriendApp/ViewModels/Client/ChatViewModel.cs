using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ChatDTO;
using RentAFriendApp.Models.ClassesDTO.MessageDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RentAFriendApp.ViewModels.Client
{
    internal class ChatViewModel : BaseViewModel, IDisposable
    {
        private readonly string _token;
        private readonly int? _targetFriendId;
        private readonly DispatcherTimer? _refreshTimer;

        #region Свойства

        private ChatListDTO? _selectedChat;
        public ChatListDTO? SelectedChat
        {
            get => _selectedChat;
            set
            {
                if (SetProperty(ref _selectedChat, value))
                {
                    OnPropertyChanged(nameof(HasSelectedChat));
                    if (value != null)
                        _ = LoadMessagesAsync(value.ChatID);
                }
            }
        }

        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                    FilterChats();
            }
        }

        private string _messageText = "";
        public string MessageText
        {
            get => _messageText;
            set
            {
                if (SetProperty(ref _messageText, value))
                    OnPropertyChanged(nameof(CanSendMessage));
            }
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

        private List<ChatListDTO> _allChats = new();

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) && SelectedChat != null;
        public bool HasSelectedChat => SelectedChat != null;
        public bool HasNoChats => FilteredChats.Count == 0;
        public bool HasNoMessages => Messages.Count == 0;

        #endregion

        #region Команды
        public ICommand SelectChatCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand RefreshCommand { get; }
        #endregion

        public ChatViewModel(string token, int? targetFriendId = null)
        {
            _token = token;
            _targetFriendId = targetFriendId;
            Title = "Сообщения";

            SelectChatCommand = new RelayCommand<ChatListDTO>(chat => SelectedChat = chat);
            SendMessageCommand = new RelayCommandAsync(SendMessageAsync, () => CanSendMessage);
            RefreshCommand = new RelayCommandAsync(LoadChatsAsync);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var user = await UserContext.GetUser(_token);
            if (user?.Data != null)
                MessageDTO.SetCurrentUserId(user.Data.UserID);

            await LoadChatsAsync();

            if (_targetFriendId.HasValue)
            {
                await OpenChatWithFriendAsync(_targetFriendId.Value);
            }
        }


        private async Task LoadChatsAsync()
        {
            try
            {
                IsBusy = true;
                var chats = await ChatContext.GetMyChats(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _allChats.Clear();
                    if (chats?.Chats != null)
                        _allChats.AddRange(chats.Chats);
                    FilterChats();
                    OnPropertyChanged(nameof(HasNoChats));
                });
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task OpenChatWithFriendAsync(int friendId)
        {
            try
            {
                IsBusy = true;
                var chat = await ChatContext.GetOrCreateChat(_token, friendId);
                if (chat != null)
                {
                    await LoadChatsAsync();
                    var existing = _allChats.FirstOrDefault(c => c.ChatID == chat.ChatId);
                    if (existing != null)
                        SelectedChat = existing;
                }
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private async Task LoadMessagesAsync(int chatId)
        {
            try
            {
                var messages = await MessageContext.GetMessages(_token, chatId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Messages.Clear();
                    if (messages?.Messages != null)
                        foreach (var m in messages.Messages)
                            Messages.Add(m);
                    OnPropertyChanged(nameof(HasNoMessages));
                });
            }
            catch (Exception ex) { SetError(ex.Message); }
        }

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage || SelectedChat == null) return;

            var text = MessageText.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                IsBusy = true;
                var dto = new SendMessageDTO
                {
                    ChatID = SelectedChat.ChatID,
                    Content = text,
                    MessageType = "Text"
                };

                var result = await MessageContext.SendMessage(_token, dto);
                if (result != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Messages.Add(new MessageDTO
                        {
                            MessageID = result.MessageId,
                            SenderID = MessageDTO.CurrentUserId,
                            Content = text,
                            MessageType = "Text",
                            IsRead = false,
                            CreatedAt = result.SentAt
                        });
                        MessageText = "";
                        OnPropertyChanged(nameof(HasNoMessages));
                    });
                }
            }
            catch (Exception ex) { SetError(ex.Message); }
            finally { IsBusy = false; }
        }

        private void FilterChats()
        {
            var q = SearchQuery?.Trim().ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(q)
                ? _allChats.ToList()
                : _allChats.Where(c =>
                    (c.InterlocutorName?.ToLower().Contains(q) ?? false) ||
                    (c.LastMessage?.ToLower().Contains(q) ?? false)).ToList();

            FilteredChats = new ObservableCollection<ChatListDTO>(filtered);
            OnPropertyChanged(nameof(HasNoChats));
        }

        public void Dispose() { }
    }
}