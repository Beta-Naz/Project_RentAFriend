using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO.Response;
using RentAFriendApp.ViewModels.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RentAFriendApp.Views.Client
{
    public partial class CatalogPage : Page
    {
        private readonly string _token;
        private CatalogViewModel _viewModel;
        private List<FPInfoDTO> _allFriends = new();
        private List<FPInfoDTO> _filteredFriends = new();
        private Border _selectedFriendCard;
        private List<string> _selectedHobbies = new();
        private double? _minRatingFilter;
        private string _cityFilter;
        private double _maxPriceFilter = 5000;
        private bool _onlyVerified = true;
        private string _availabilityFilter = "any";
        private bool isActive = false;

        public CatalogPage(string token)
        {
            InitializeComponent();
            _token = token;
            isActive = true;

            // Инициализация ViewModel
            _viewModel = new CatalogViewModel(_token);
            DataContext = _viewModel;

            LoadFriendsFromDatabase();

            // Подписка на события ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void LoadFriendsFromDatabase()
        {
            try
            {
                _allFriends.Clear();

                // Получаем все профили через контекст
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);

                if (profilesResponse?.Profiles != null)
                {
                    _allFriends = profilesResponse.Profiles.ToList();
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каталога: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            LoadCitiesForFilter();
            _filteredFriends = _allFriends.Where(friend =>
            {
                // Фильтр по городу
                if (!string.IsNullOrEmpty(_cityFilter) && _cityFilter != "Все города" && friend.City != _cityFilter)
                    return false;

                // Фильтр по цене
                if (friend.HourlyRate.HasValue && friend.HourlyRate.Value > (decimal)_maxPriceFilter)
                    return false;

                // Фильтр по рейтингу
                if (_minRatingFilter.HasValue && friend.AverageRating.HasValue &&
                    friend.AverageRating.Value < (decimal)_minRatingFilter.Value)
                    return false;

                // Фильтр по верификации
                if (_onlyVerified && !friend.IsVerified)
                    return false;

                // Фильтр по хобби
                if (_selectedHobbies.Any())
                {
                    if (string.IsNullOrEmpty(friend.Hobbies))
                        return false;

                    var friendHobbies = friend.Hobbies.Split(',')
                        .Select(h => h.Trim().ToLower())
                        .ToList();

                    if (!_selectedHobbies.Any(h => friendHobbies.Contains(h.ToLower())))
                        return false;
                }

                return true;
            }).ToList();

            ApplySorting();
            DisplayFriends();
        }

        private void ApplySorting()
        {
            switch ((SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString())
            {
                case "По цене (дешевле)":
                    _filteredFriends = _filteredFriends
                        .OrderBy(f => f.HourlyRate ?? decimal.MaxValue)
                        .ToList();
                    break;

                case "По цене (дороже)":
                    _filteredFriends = _filteredFriends
                        .OrderByDescending(f => f.HourlyRate ?? decimal.MinValue)
                        .ToList();
                    break;

                case "По новизне":
                    _filteredFriends = _filteredFriends
                        .OrderByDescending(f => f.ProfileID)
                        .ToList();
                    break;

                case "По рейтингу":
                default:
                    _filteredFriends = _filteredFriends
                        .OrderByDescending(f => f.AverageRating ?? 0)
                        .ToList();
                    break;
            }
        }

        private void DisplayFriends()
        {
            if (FriendsPanel == null)
            {
                MessageBox.Show("Ошибка: FriendsPanel не был инициализирован.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            FriendsPanel.Children.Clear();

            if (!_filteredFriends.Any())
            {
                ShowNoResultsMessage();
                return;
            }

            foreach (var friend in _filteredFriends)
            {
                var card = CreateFriendCard(friend);
                FriendsPanel.Children.Add(card);
            }
        }

        private void ShowNoResultsMessage()
        {
            var noResultsBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(40),
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var icon = new System.Windows.Shapes.Path
            {
                Data = (Geometry)FindResource("SearchIcon"),
                Fill = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                Width = 48,
                Height = 48,
                Margin = new Thickness(0, 0, 0, 16),
                Stretch = Stretch.Uniform
            };

            var text = new TextBlock
            {
                Text = "По вашему запросу ничего не найдено\nПопробуйте изменить фильтры",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(text);
            noResultsBorder.Child = stackPanel;
            FriendsPanel.Children.Add(noResultsBorder);
        }

        private Border CreateFriendCard(FPInfoDTO friend)
        {
            var card = new Border
            {
                Style = (Style)FindResource("FriendCardStyle"),
                Width = 280,
                Tag = friend.ProfileID
            };

            card.MouseDown += FriendCard_MouseDown;
            card.MouseEnter += FriendCard_MouseEnter;
            card.MouseLeave += FriendCard_MouseLeave;

            var stackPanel = new StackPanel();

            var avatarGrid = CreateAvatarSection(friend);
            stackPanel.Children.Add(avatarGrid);

            var description = new TextBlock
            {
                Text = string.IsNullOrEmpty(friend.Bio) ? "Нет описания" :
                       (friend.Bio.Length > 120 ? friend.Bio.Substring(0, 120) + "..." : friend.Bio),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 60,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(description);

            if (!string.IsNullOrEmpty(friend.Hobbies))
            {
                var hobbiesPanel = CreateHobbiesPanel(friend.Hobbies);
                stackPanel.Children.Add(hobbiesPanel);
            }

            var priceGrid = CreatePriceAndButtonSection(friend);
            stackPanel.Children.Add(priceGrid);

            card.Child = stackPanel;
            return card;
        }

        private Grid CreateAvatarSection(FPInfoDTO friend)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var avatarContainer = new Grid();

            var avatarBorder = new Border
            {
                Width = 80,
                Height = 80,
                Background = new SolidColorBrush(Color.FromArgb(255, 232, 245, 232)),
                CornerRadius = new CornerRadius(40)
            };

            var initials = GetInitials(friend.FullName);
            var avatarText = new TextBlock
            {
                Text = initials,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            avatarBorder.Child = avatarText;
            avatarContainer.Children.Add(avatarBorder);

            if (friend.IsVerified)
            {
                var verifiedBorder = new Border
                {
                    Width = 25,
                    Height = 25,
                    Background = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                var verifiedIcon = new System.Windows.Shapes.Path
                {
                    Data = (Geometry)FindResource("VerifiedIcon"),
                    Fill = Brushes.White,
                    Width = 22,
                    Height = 22,
                    Stretch = Stretch.Uniform
                };
                verifiedBorder.Child = verifiedIcon;
                avatarContainer.Children.Add(verifiedBorder);
            }

            Grid.SetColumn(avatarContainer, 0);
            grid.Children.Add(avatarContainer);

            var infoPanel = new StackPanel
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = friend.FullName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            infoPanel.Children.Add(nameText);

            var detailsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };

            if (!string.IsNullOrEmpty(friend.City))
            {
                var locationIcon = new System.Windows.Shapes.Path
                {
                    Data = (Geometry)FindResource("LocationIcon"),
                    Fill = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Width = 24,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                };
                detailsPanel.Children.Add(locationIcon);

                var cityText = new TextBlock
                {
                    Text = friend.City,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(4, 0, 12, 0)
                };
                detailsPanel.Children.Add(cityText);
            }

            if (friend.AverageRating.HasValue)
            {
                var starIcon = new System.Windows.Shapes.Path
                {
                    Data = (Geometry)FindResource("StarIcon"),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    Width = 24,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                };
                detailsPanel.Children.Add(starIcon);

                var ratingText = new TextBlock
                {
                    Text = friend.AverageRating.Value.ToString("0.0"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    Margin = new Thickness(4, 0, 0, 0)
                };
                detailsPanel.Children.Add(ratingText);
            }

            infoPanel.Children.Add(detailsPanel);
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            return grid;
        }

        private WrapPanel CreateHobbiesPanel(string hobbies)
        {
            var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };

            var hobbyList = hobbies.Split(',')
                .Select(h => h.Trim())
                .Take(3)
                .ToList();

            foreach (var hobby in hobbyList)
            {
                var hobbyBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 240, 249, 240)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 6, 6)
                };

                var hobbyText = new TextBlock
                {
                    Text = hobby,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 46, 125, 50))
                };

                hobbyBorder.Child = hobbyText;
                wrapPanel.Children.Add(hobbyBorder);
            }

            return wrapPanel;
        }

        private Grid CreatePriceAndButtonSection(FPInfoDTO friend)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pricePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var priceText = new TextBlock
            {
                Text = friend.HourlyRate.HasValue ? $"{friend.HourlyRate.Value:N0} ₽" : "Цена не указана",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            pricePanel.Children.Add(priceText);

            var perHourText = new TextBlock
            {
                Text = "за час",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
            };
            pricePanel.Children.Add(perHourText);

            Grid.SetColumn(pricePanel, 0);
            grid.Children.Add(pricePanel);

            var selectButton = new Button
            {
                Content = "Выбрать",
                Background = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Height = 36,
                Padding = new Thickness(20, 0, 20, 0),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = friend.ProfileID
            };
            selectButton.Click += SelectButton_Click;

            Grid.SetColumn(selectButton, 1);
            grid.Children.Add(selectButton);

            return grid;
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            }

            return "??";
        }

        private async void LoadCitiesForFilter()
        {
            CityComboBox.Items.Clear();
            CityComboBox.Items.Add("Все города");

            try
            {
                var cities = _viewModel.AvailableCities;
                if (cities != null)
                {
                    foreach (var city in cities)
                    {
                        CityComboBox.Items.Add(city);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки городов: {ex.Message}");
            }
        }

        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var clientHomePage = new ClientHomePage(_token);
                mainWindow.MainFrame.Navigate(clientHomePage);
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Найти друга по имени, хобби или городу...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Найти друга по имени, хобби или городу...";
                SearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox.Text;
            if (!string.IsNullOrWhiteSpace(searchText) &&
                searchText != "Найти друга по имени, хобби или городу...")
            {
                _filteredFriends = _filteredFriends.Where(friend =>
                    friend.FullName.ToLower().Contains(searchText.ToLower()) ||
                    (friend.City?.ToLower().Contains(searchText.ToLower()) ?? false) ||
                    (friend.Hobbies?.ToLower().Contains(searchText.ToLower()) ?? false) ||
                    (friend.Bio?.ToLower().Contains(searchText.ToLower()) ?? false)
                ).ToList();

                DisplayFriends();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isActive)
            {
                ApplySorting();
                DisplayFriends();
            }
        }

        private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CityComboBox.SelectedItem is ComboBoxItem selectedItem && isActive)
            {
                _cityFilter = selectedItem.Content.ToString() == "Все города" ? null : selectedItem.Content.ToString();
                ApplyFilters();
            }
        }

        private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isActive)
            {
                _maxPriceFilter = e.NewValue;
                PriceValueText.Text = $"{_maxPriceFilter:N0} ₽";
            }
        }

        private void RatingButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button == null) return;

            if (button != Rating45Button) Rating45Button.IsChecked = false;
            if (button != Rating40Button) Rating40Button.IsChecked = false;
            if (button != Rating35Button) Rating35Button.IsChecked = false;
            if (button != Rating30Button) Rating30Button.IsChecked = false;

            if (button == Rating45Button) _minRatingFilter = 4.5;
            else if (button == Rating40Button) _minRatingFilter = 4.0;
            else if (button == Rating35Button) _minRatingFilter = 3.5;
            else if (button == Rating30Button) _minRatingFilter = 3.0;
            else _minRatingFilter = null;
        }

        private void RatingButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!Rating45Button.IsChecked.GetValueOrDefault() &&
                !Rating40Button.IsChecked.GetValueOrDefault() &&
                !Rating35Button.IsChecked.GetValueOrDefault() &&
                !Rating30Button.IsChecked.GetValueOrDefault())
            {
                _minRatingFilter = null;
            }
        }

        private void VerifiedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isActive)
            {
                _onlyVerified = true;
                ApplyFilters();
            }
        }

        private void VerifiedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _onlyVerified = false;
            ApplyFilters();
        }

        private void HobbyButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button != null && button.Content is string hobby)
            {
                _selectedHobbies.Add(hobby);
                ApplyFilters();
            }
        }

        private void HobbyButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button != null && button.Content is string hobby)
            {
                _selectedHobbies.Remove(hobby);
                ApplyFilters();
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            CityComboBox.SelectedIndex = 0;
            PriceSlider.Value = 5000;

            Rating45Button.IsChecked = false;
            Rating40Button.IsChecked = false;
            Rating35Button.IsChecked = false;
            Rating30Button.IsChecked = false;

            VerifiedCheckBox.IsChecked = true;

            _selectedHobbies.Clear();
            SportHobbyButton.IsChecked = false;
            MovieHobbyButton.IsChecked = false;
            MusicHobbyButton.IsChecked = false;
            TravelHobbyButton.IsChecked = false;
            ArtHobbyButton.IsChecked = false;
            PhotoHobbyButton.IsChecked = false;
            CookingHobbyButton.IsChecked = false;
            TheaterHobbyButton.IsChecked = false;

            AnyTimeRadioButton.IsChecked = true;

            _cityFilter = null;
            _maxPriceFilter = 5000;
            _minRatingFilter = null;
            _onlyVerified = true;
            _selectedHobbies.Clear();
            _availabilityFilter = "any";

            SearchTextBox.Text = "Найти друга по имени, хобби или городу...";
            SearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));

            ApplyFilters();
        }

        private void FriendCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is int profileId)
            {
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                {
                    OpenFriendProfile(profileId);
                }
                else if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
                {
                    if (_selectedFriendCard != null)
                    {
                        _selectedFriendCard.Background = Brushes.White;
                    }

                    card.Background = new SolidColorBrush(Color.FromArgb(30, 76, 175, 80));
                    _selectedFriendCard = card;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    ShowFriendContextMenu(card, e.GetPosition(card));
                }
            }
        }

        private void FriendCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border card)
            {
                card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20,
                    Opacity = 0.15,
                    ShadowDepth = 2,
                    Color = Colors.Black
                };
            }
        }

        private void FriendCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border card && card != _selectedFriendCard)
            {
                card.Effect = null;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int profileId)
            {
                OpenFriendProfile(profileId);
            }
        }

        private async void OpenFriendProfile(int profileId)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var friendDetailsPage = new FriendDetailsPage(_token, profileId);
                mainWindow.MainFrame.Navigate(friendDetailsPage);
            }
        }

        private async void OpenChatWichFriend(int friendId)
        {
           var mainWindow = Window.GetWindow(this) as MainWindow;
           if (mainWindow != null)
           {
               var chatPage = new ChatPage(_token, friendId);
               mainWindow.MainFrame.Navigate(chatPage);
           }
        }

        private void ShowFriendContextMenu(Border card, Point position)
        {
            try
            {
                var contextMenu = new ContextMenu();

                var detailsItem = new MenuItem
                {
                    Header = "Просмотреть профиль",
                    Tag = card.Tag
                };
                detailsItem.Click += (s, e) => OpenFriendProfile((int)card.Tag);

                var messageItem = new MenuItem
                {
                    Header = "Написать сообщение",
                    Tag = card.Tag
                };
                messageItem.Click += (s, e) => OpenChatWichFriend((int)card.Tag);

                contextMenu.Items.Add(detailsItem);
                contextMenu.Items.Add(messageItem);
                contextMenu.Items.Add(new Separator());

                var addToFavoritesItem = new MenuItem
                {
                    Header = "Добавить в избранное"
                };
                contextMenu.Items.Add(addToFavoritesItem);

                contextMenu.PlacementTarget = card;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Initialize();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Cleanup();
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Обработка изменений ViewModel
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void FilterScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

        }

        private void AvailabilityRadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void FriendsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

        }
    }
}