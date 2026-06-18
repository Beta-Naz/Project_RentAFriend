using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    public class CatalogViewModel : INotifyPropertyChanged
    {
        public readonly string Token;
        private List<FPInfoDTO> _allFriends = new();

        public event Action? FriendsChanged;

        private ObservableCollection<FPInfoDTO> _filteredFriends = new();
        public ObservableCollection<FPInfoDTO> FilteredFriends
        {
            get => _filteredFriends;
            set { _filteredFriends = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _availableCities = new();
        public ObservableCollection<string> AvailableCities
        {
            get => _availableCities;
            set { _availableCities = value; OnPropertyChanged(); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        private string _selectedCity = "Все города";
        public string SelectedCity
        {
            get => _selectedCity;
            set { _selectedCity = value; OnPropertyChanged(); ApplyFilters(); }
        }

        private double _maxPrice = 5000;
        public double MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(); ApplyFilters(); }
        }

        private bool _onlyVerified = true;
        public bool OnlyVerified
        {
            get => _onlyVerified;
            set { _onlyVerified = value; OnPropertyChanged(); ApplyFilters(); }
        }

        private string _selectedRating = "Any";
        public string SelectedRating
        {
            get => _selectedRating;
            set
            {
                _selectedRating = value;
                OnPropertyChanged();

                RatingAny = (value == "Any");
                Rating45 = (value == "45");
                Rating40 = (value == "40");
                Rating30 = (value == "30");

                ApplyFilters();
            }
        }

        private bool _ratingAny = true;
        public bool RatingAny
        {
            get => _ratingAny;
            set
            {
                if (_ratingAny != value)
                {
                    _ratingAny = value;
                    OnPropertyChanged();
                    if (value) SelectedRating = "Any";
                }
            }
        }

        private bool _rating45;
        public bool Rating45
        {
            get => _rating45;
            set
            {
                if (_rating45 != value)
                {
                    _rating45 = value;
                    OnPropertyChanged();
                    if (value) SelectedRating = "45";
                }
            }
        }

        private bool _rating40;
        public bool Rating40
        {
            get => _rating40;
            set
            {
                if (_rating40 != value)
                {
                    _rating40 = value;
                    OnPropertyChanged();
                    if (value) SelectedRating = "40";
                }
            }
        }

        private bool _rating30;
        public bool Rating30
        {
            get => _rating30;
            set
            {
                if (_rating30 != value)
                {
                    _rating30 = value;
                    OnPropertyChanged();
                    if (value) SelectedRating = "30";
                }
            }
        }

        private HashSet<string> _selectedHobbies = new();
        public HashSet<string> SelectedHobbies => _selectedHobbies;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand SearchCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ToggleHobbyCommand { get; }
        public ICommand ViewFriendCommand { get; }

        public CatalogViewModel(string token)
        {
            Token = token;

            SearchCommand = new RelayCommandAsync(ApplyFilters);
            ResetCommand = new RelayCommandAsync(ResetFilters);
            ToggleHobbyCommand = new RelayCommandAsync<string>(ToggleHobby);
            ViewFriendCommand = new RelayCommandAsync<object>(ViewFriend);
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var response = await FriendProfileContext.GetAllProfiles(Token);
                if (response?.Profiles != null)
                {
                    _allFriends = response.Profiles.ToList();

                    // Города
                    var cities = _allFriends.Select(f => f.City).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AvailableCities = new ObservableCollection<string>(new[] { "Все города" }.Concat(cities));
                    });

                    await ApplyFilters();
                }
            }
            finally { IsBusy = false; }
        }

        public Task ApplyFilters()
        {
            var filtered = _allFriends.AsEnumerable();

            // Город
            if (!string.IsNullOrWhiteSpace(SelectedCity) && SelectedCity != "Все города")
                filtered = filtered.Where(f => f.City == SelectedCity);

            // Цена
            filtered = filtered.Where(f => f.HourlyRate <= (decimal)MaxPrice);

            // Верификация
            if (OnlyVerified)
                filtered = filtered.Where(f => f.IsVerified);

            // Рейтинг (используем SelectedRating)
            switch (SelectedRating)
            {
                case "45":
                    filtered = filtered.Where(f => f.AverageRating >= 4.5m);
                    break;
                case "40":
                    filtered = filtered.Where(f => f.AverageRating >= 4.0m);
                    break;
                case "30":
                    filtered = filtered.Where(f => f.AverageRating >= 3.0m);
                    break;
                case "Any":
                default:
                    // Без фильтрации по рейтингу
                    break;
            }

            // Хобби
            if (_selectedHobbies.Count > 0)
                filtered = filtered.Where(f => !string.IsNullOrEmpty(f.Hobbies) &&
                    _selectedHobbies.Any(h => f.Hobbies.Split(',').Select(x => x.Trim()).Contains(h, StringComparer.OrdinalIgnoreCase)));

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchText) && SearchText != "Найти друга по имени, хобби или городу...")
                filtered = filtered.Where(f =>
                    (f.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.City?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (f.Hobbies?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

            // Сортировка по рейтингу
            filtered = filtered.OrderByDescending(f => f.AverageRating ?? 0);

            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredFriends = new ObservableCollection<FPInfoDTO>(filtered);
                FriendsChanged?.Invoke();
            });
            return Task.CompletedTask;
        }

        private Task ResetFilters()
        {
            SelectedCity = "Все города";
            MaxPrice = 5000;
            OnlyVerified = true;
            SelectedRating = "Any";
            _selectedHobbies.Clear();
            SearchText = "";
            ApplyFilters();
            return Task.CompletedTask;
        }

        private Task ToggleHobby(string? hobby)
        {
            if (string.IsNullOrEmpty(hobby)) return Task.CompletedTask;
            if (_selectedHobbies.Contains(hobby))
                _selectedHobbies.Remove(hobby);
            else
                _selectedHobbies.Add(hobby);
            ApplyFilters();
            return Task.CompletedTask;
        }

        private Task ViewFriend(object? friend)
        {
            if (friend is FPInfoDTO f)
            {
                var main = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                main?.MainFrame.Navigate(new Views.Client.FriendDetailsPage(Token, f.ProfileID));
            }
            return Task.CompletedTask;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}