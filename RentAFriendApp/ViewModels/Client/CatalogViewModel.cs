using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
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

        // ===== КОЛЛЕКЦИИ =====
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

        // ===== ФИЛЬТРЫ =====
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

        private bool _rating45;
        public bool Rating45 { get => _rating45; set { _rating45 = value; OnPropertyChanged(); ApplyFilters(); } }
        private bool _rating40;
        public bool Rating40 { get => _rating40; set { _rating40 = value; OnPropertyChanged(); ApplyFilters(); } }
        private bool _rating30;
        public bool Rating30 { get => _rating30; set { _rating30 = value; OnPropertyChanged(); ApplyFilters(); } }

        private HashSet<string> _selectedHobbies = new();
        public HashSet<string> SelectedHobbies => _selectedHobbies;

        // ===== СОСТОЯНИЕ =====
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        // ===== КОМАНДЫ =====
        public ICommand SearchCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ToggleHobbyCommand { get; }
        public ICommand ViewFriendCommand { get; }

        public CatalogViewModel(string token)
        {
            Token = token;

            SearchCommand = new RelayCommand(ApplyFilters);
            ResetCommand = new RelayCommand(ResetFilters);
            ToggleHobbyCommand = new RelayCommand<string>(ToggleHobby);
            ViewFriendCommand = new RelayCommand<object>(ViewFriend);
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

                    ApplyFilters();
                }
            }
            finally { IsBusy = false; }
        }

        public void ApplyFilters()
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

            // Рейтинг
            if (Rating45) filtered = filtered.Where(f => f.AverageRating >= 4.5m);
            else if (Rating40) filtered = filtered.Where(f => f.AverageRating >= 4.0m);
            else if (Rating30) filtered = filtered.Where(f => f.AverageRating >= 3.0m);

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
        }

        private void ResetFilters()
        {
            SelectedCity = "Все города";
            MaxPrice = 5000;
            OnlyVerified = true;
            Rating45 = false;
            Rating40 = false;
            Rating30 = false;
            _selectedHobbies.Clear();
            SearchText = "";
            ApplyFilters();
        }

        private void ToggleHobby(string? hobby)
        {
            if (string.IsNullOrEmpty(hobby)) return;
            if (_selectedHobbies.Contains(hobby))
                _selectedHobbies.Remove(hobby);
            else
                _selectedHobbies.Add(hobby);
            ApplyFilters();
        }

        private void ViewFriend(object? friend)
        {
            if (friend is FPInfoDTO f)
            {
                var main = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                main?.MainFrame.Navigate(new Views.Client.FriendDetailsPage(Token, f.ProfileID));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? p) => true;
        public void Execute(object? p) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        public RelayCommand(Action<T?> execute) => _execute = execute;
        public bool CanExecute(object? p) => true;
        public void Execute(object? p) => _execute((T?)p);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}