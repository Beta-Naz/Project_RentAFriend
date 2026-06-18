using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using RentAFriendApp.Models.ClassesDTO.ChatDTO;
using RentAFriendApp.ViewModels.Client;

namespace RentAFriendApp.Views.Client
{
    public partial class ChatPage : Page
    {
        private readonly ChatViewModel _viewModel;
        private readonly int? _targetFriendId;

        public ChatPage(string token, int friendId = -1)
        {
            InitializeComponent();
            _targetFriendId = friendId > 0 ? friendId : null;
            _viewModel = new ChatViewModel(token, _targetFriendId);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChatViewModel.Messages))
                    MessagesScrollViewer?.ScrollToEnd();
            };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void ChatItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ChatListDTO chat)
            {
                _viewModel.SelectChatCommand.Execute(chat);
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Control)
            {
                if (_viewModel.CanSendMessage)
                {
                    _viewModel.SendMessageCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Dispose();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}