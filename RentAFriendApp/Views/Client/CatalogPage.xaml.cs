using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Client;
using System.Windows;
using System.Windows.Controls;

namespace RentAFriendApp.Views.Client
{
    public partial class CatalogPage : Page
    {
        private readonly CatalogViewModel _viewModel;

        public CatalogPage(string token)
        {
            InitializeComponent();
            _viewModel = new CatalogViewModel(token);
            DataContext = _viewModel;

            _viewModel.FriendsChanged += OnFriendsChanged;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsBusy))
                    LoadingPanel.Visibility = _viewModel.IsBusy ? Visibility.Visible : Visibility.Collapsed;
            };

            _ = _viewModel.LoadAsync();
        }

        private void OnFriendsChanged()
        {
            FriendsPanel.Children.Clear();

            if (_viewModel.FilteredFriends.Count == 0)
            {
                NoResultsPanel.Visibility = Visibility.Visible;
                return;
            }

            NoResultsPanel.Visibility = Visibility.Collapsed;

            foreach (var friend in _viewModel.FilteredFriends)
            {
                var card = new Views.Controls.FriendCard();
                card.SetFriend(friend);
                card.ViewRequested += (f) => OpenFriendProfile(f.ProfileID);
                card.Width = 280;
                card.Margin = new Thickness(6);
                FriendsPanel.Children.Add(card);
            }
        }

        private void OpenFriendProfile(int profileId)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.MainFrame.Navigate(new FriendDetailsPage(_viewModel.Token, profileId));
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => _viewModel.SearchCommand.Execute(null);
        private void ResetButton_Click(object sender, RoutedEventArgs e) => _viewModel.ResetCommand.Execute(null);
    }
}