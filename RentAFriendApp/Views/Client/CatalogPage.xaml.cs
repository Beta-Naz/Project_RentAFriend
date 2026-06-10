using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Client;
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
        private readonly CatalogViewModel _viewModel;
        private List<FPInfoDTO> _allFriends = [];
        private List<FPInfoDTO> _filteredFriends = [];
        private Border? _selectedFriendCard;
        private readonly List<string> _selectedHobbies = [];
        private double? _minRatingFilter;
        private string? _cityFilter;
        private double _maxPriceFilter = 5000;
        private bool _onlyVerified = true;
        private readonly bool _isActive;

        public CatalogPage(string token)
        {
            InitializeComponent();
            _token = token;
            _isActive = true;

            _viewModel = new CatalogViewModel(_token);
            DataContext = _viewModel;

            LoadFriendsFromDatabase();

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void LoadFriendsFromDatabase()
        {
            try
            {
                _allFriends.Clear();

                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);

                if (profilesResponse?.Profiles != null)
                {
                    _allFriends = [.. profilesResponse.Profiles];
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
            _filteredFriends = [.. _allFriends.Where(friend =>
            {
                if (!string.IsNullOrEmpty(_cityFilter) && _cityFilter != "Все города" && friend.City != _cityFilter)
                    return false;

                if (friend.HourlyRate.HasValue && friend.HourlyRate.Value > (decimal)_maxPriceFilter)
                    return false;

                if (_minRatingFilter.HasValue && friend.AverageRating.HasValue &&
                    friend.AverageRating.Value < (decimal)_minRatingFilter.Value)
                    return false;

                if (_onlyVerified && !friend.IsVerified)
                    return false;

                if (_selectedHobbies.Count != 0)
                {
                    if (string.IsNullOrEmpty(friend.Hobbies))
                        return false;

                    var friendHobbies = friend.Hobbies.Split(',')
                        .Select(h => h.Trim())
                        .ToList();

                    if (!_selectedHobbies.Any(h => friendHobbies.Contains(h, StringComparer.OrdinalIgnoreCase)))
                        return false;
                }

                return true;
            })];

            ApplySorting();
            DisplayFriends();
        }

        private void ApplySorting()
        {
            var sortOption = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            _filteredFriends = sortOption switch
            {
                "По цене (дешевле)" => [.. _filteredFriends.OrderBy(f => f.HourlyRate ?? decimal.MaxValue)],
                "По цене (дороже)" => [.. _filteredFriends.OrderByDescending(f => f.HourlyRate ?? decimal.MinValue)],
                "По новизне" => [.. _filteredFriends.OrderByDescending(f => f.ProfileID)],
                _ => [.. _filteredFriends.OrderByDescending(f => f.AverageRating ?? 0)]
            };
        }

        private void DisplayFriends()
        {
            if (FriendsPanel == null) return;

            FriendsPanel.Children.Clear();

            if (_filteredFriends.Count == 0)
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
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new System.Windows.Shapes.Path
                        {
                            Data = (Geometry)FindResource("SearchIcon"),
                            Fill = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                            Width = 48,
                            Height = 48,
                            Margin = new Thickness(0, 0, 0, 16),
                            Stretch = Stretch.Uniform
                        },
                        new TextBlock
                        {
                            Text = "По вашему запросу ничего не найдено\nПопробуйте изменить фильтры",
                            FontSize = 16,
                            Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            };

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

            var bioText = string.IsNullOrEmpty(friend.Bio) ? "Нет описания" :
                       friend.Bio.Length > 120 ? string.Concat(friend.Bio.AsSpan(0, 120), "...") : friend.Bio;

            var description = new TextBlock
            {
                Text = bioText,
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
                CornerRadius = new CornerRadius(40),
                Child = new TextBlock
                {
                    Text = GetInitials(friend.FullName),
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
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
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Child = new System.Windows.Shapes.Path
                    {
                        Data = (Geometry)FindResource("VerifiedIcon"),
                        Fill = Brushes.White,
                        Width = 22,
                        Height = 22,
                        Stretch = Stretch.Uniform
                    }
                };
                avatarContainer.Children.Add(verifiedBorder);
            }

            Grid.SetColumn(avatarContainer, 0);
            grid.Children.Add(avatarContainer);

            var infoPanel = new StackPanel
            {
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = friend.FullName,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    }
                }
            };

            var detailsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };

            if (!string.IsNullOrEmpty(friend.City))
            {
                detailsPanel.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = (Geometry)FindResource("LocationIcon"),
                    Fill = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Width = 24,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                });

                detailsPanel.Children.Add(new TextBlock
                {
                    Text = friend.City,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    Margin = new Thickness(4, 0, 12, 0)
                });
            }

            if (friend.AverageRating.HasValue)
            {
                detailsPanel.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = (Geometry)FindResource("StarIcon"),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    Width = 24,
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                });

                detailsPanel.Children.Add(new TextBlock
                {
                    Text = friend.AverageRating.Value.ToString("0.0"),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    Margin = new Thickness(4, 0, 0, 0)
                });
            }

            infoPanel.Children.Add(detailsPanel);
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            return grid;
        }

        private static WrapPanel CreateHobbiesPanel(string hobbies)
        {
            var wrapPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };

            var hobbyList = hobbies.Split(',')
                .Select(h => h.Trim())
                .Take(3);

            foreach (var hobby in hobbyList)
            {
                wrapPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 240, 249, 240)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 6, 6),
                    Child = new TextBlock
                    {
                        Text = hobby,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 46, 125, 50))
                    }
                });
            }

            return wrapPanel;
        }

        private Grid CreatePriceAndButtonSection(FPInfoDTO friend)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pricePanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = friend.HourlyRate.HasValue ? $"{friend.HourlyRate.Value:N0} ₽" : "Цена не указана",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
                    },
                    new TextBlock
                    {
                        Text = "за час",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
                    }
                }
            };

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

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }

            return parts.Length == 1 && parts[0].Length >= 2
                ? parts[0][..2].ToUpper()
                : parts[0].ToUpper();
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

            await Task.CompletedTask;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.MainFrame.Navigate(new ClientHomePage(_token));
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
            if (string.IsNullOrWhiteSpace(searchText) ||
                searchText == "Найти друга по имени, хобби или городу...")
                return;

            _filteredFriends = [.. _filteredFriends.Where(friend =>
                friend.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (friend.City?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (friend.Hobbies?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (friend.Bio?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            )];

            DisplayFriends();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isActive)
            {
                ApplySorting();
                DisplayFriends();
            }
        }

        private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CityComboBox.SelectedItem is ComboBoxItem selectedItem && _isActive)
            {
                _cityFilter = selectedItem.Content.ToString() == "Все города" ? null : selectedItem.Content.ToString();
                ApplyFilters();
            }
        }

        private void PriceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isActive)
            {
                _maxPriceFilter = e.NewValue;
                PriceValueText.Text = $"{_maxPriceFilter:N0} ₽";
            }
        }

        private void RatingButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button) return;

            if (button != Rating45Button) Rating45Button.IsChecked = false;
            if (button != Rating40Button) Rating40Button.IsChecked = false;
            if (button != Rating35Button) Rating35Button.IsChecked = false;
            if (button != Rating30Button) Rating30Button.IsChecked = false;

            _minRatingFilter = button switch
            {
                _ when button == Rating45Button => 4.5,
                _ when button == Rating40Button => 4.0,
                _ when button == Rating35Button => 3.5,
                _ when button == Rating30Button => 3.0,
                _ => null
            };
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
            if (_isActive)
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
            if (sender is ToggleButton { Content: string hobby })
            {
                _selectedHobbies.Add(hobby);
                ApplyFilters();
            }
        }

        private void HobbyButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton { Content: string hobby })
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

            SearchTextBox.Text = "Найти друга по имени, хобби или городу...";
            SearchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));

            ApplyFilters();
        }

        private void FriendCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border { Tag: int profileId } card)
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
                    ShowFriendContextMenu(card);
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
            if (sender is Button { Tag: int profileId })
            {
                OpenFriendProfile(profileId);
            }
        }

        private void OpenFriendProfile(int profileId)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.MainFrame.Navigate(new FriendDetailsPage(_token, profileId));
        }

        private void OpenChatWichFriend(int friendId)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.MainFrame.Navigate(new ChatPage(_token, friendId));
        }

        private void ShowFriendContextMenu(Border card)
        {
            try
            {
                var contextMenu = new ContextMenu();

                var detailsItem = new MenuItem
                {
                    Header = "Просмотреть профиль",
                    Tag = card.Tag
                };
                detailsItem.Click += (_, _) => OpenFriendProfile((int)card.Tag);

                var messageItem = new MenuItem
                {
                    Header = "Написать сообщение",
                    Tag = card.Tag
                };
                messageItem.Click += (_, _) => OpenChatWichFriend((int)card.Tag);

                contextMenu.Items.Add(detailsItem);
                contextMenu.Items.Add(messageItem);
                contextMenu.Items.Add(new Separator());
                contextMenu.Items.Add(new MenuItem { Header = "Добавить в избранное" });

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

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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