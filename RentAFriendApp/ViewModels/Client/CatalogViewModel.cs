using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Client
{
    internal class CatalogViewModel : BaseViewModel
    {
        private readonly string _token;

        private string _selectedCity = "Все города";
        public string SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (SetProperty(ref _selectedCity, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private decimal? _maxHourlyRate;
        public decimal? MaxHourlyRate
        {
            get => _maxHourlyRate;
            set
            {
                if (SetProperty(ref _maxHourlyRate, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private bool _onlyVerified = true;
        public bool OnlyVerified
        {
            get => _onlyVerified;
            set
            {
                if (SetProperty(ref _onlyVerified, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private decimal? _minRating;
        public decimal? MinRating
        {
            get => _minRating;
            set
            {
                if (SetProperty(ref _minRating, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private string _sortBy = "Rating";
        private string _sortOrder = "DESC";

        // Коллекции
        private ObservableCollection<FPInfoDTO> _friends;
        public ObservableCollection<FPInfoDTO> Friends
        {
            get => _friends;
            set => SetProperty(ref _friends, value);
        }

        private ObservableCollection<string> _availableCities;
        public ObservableCollection<string> AvailableCities
        {
            get => _availableCities;
            set => SetProperty(ref _availableCities, value);
        }

        private FPInfoDTO _selectedFriend;
        public FPInfoDTO SelectedFriend
        {
            get => _selectedFriend;
            set
            {
                if (SetProperty(ref _selectedFriend, value) && value != null)
                {
                    ViewFriendDetails(value);
                }
            }
        }

        // Команды
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ViewFriendDetailsCommand { get; }
        public ICommand SortByPriceCommand { get; }
        public ICommand SortByRatingCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand RefreshCommand { get; }

        public CatalogViewModel(string token)
        {
            _token = token;
            Title = "Каталог друзей";

            Friends = new ObservableCollection<FPInfoDTO>();
            AvailableCities = new ObservableCollection<string>();

            // Инициализация команд
            ApplyFiltersCommand = new RelayCommandAsync(ApplyFiltersAsync);
            ResetFiltersCommand = new RelayCommandAsync(ResetFiltersAsync);
            ViewFriendDetailsCommand = new RelayCommandAsync<FPInfoDTO>(ViewFriendDetails);
            SortByPriceCommand = new RelayCommandAsync(() => SortByAsync("Price"));
            SortByRatingCommand = new RelayCommandAsync(() => SortByAsync("Rating"));
            SortByNameCommand = new RelayCommandAsync(() => SortByAsync("Name"));
            RefreshCommand = new RelayCommandAsync(LoadItemsAsync);

            _ = LoadItemsAsync();
        }
        private async Task LoadItemsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var allProfiles = await FriendProfileContext.GetAllProfiles(_token);

                if (allProfiles?.Profiles == null)
                {
                    SetError("Не удалось загрузить список друзей");
                    return;
                }

                // Загружаем города только один раз
                if (AvailableCities.Count == 0)
                {
                    var cities = allProfiles.Profiles
                        .Select(p => p.City)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    if(cities != null)
                    {
                        await LoadCitiesAsync(cities);
                    }
                }

                var filtered = allProfiles.Profiles.AsEnumerable();

                // Фильтры
                if (!string.IsNullOrEmpty(SelectedCity) && SelectedCity != "Все города")
                {
                    filtered = filtered.Where(f => f.City == SelectedCity);
                }

                if (MaxHourlyRate.HasValue)
                {
                    filtered = filtered.Where(f => f.HourlyRate <= MaxHourlyRate.Value);
                }

                if (OnlyVerified)
                {
                    filtered = filtered.Where(f => f.IsVerified);
                }

                if (MinRating.HasValue)
                {
                    filtered = filtered.Where(f => f.AverageRating >= MinRating.Value);
                }

                // Сортировка
                filtered = _sortBy switch
                {
                    "Price" => _sortOrder == "ASC"
                        ? filtered.OrderBy(f => f.HourlyRate)
                        : filtered.OrderByDescending(f => f.HourlyRate),
                    "Name" => _sortOrder == "ASC"
                        ? filtered.OrderBy(f => f.FullName)
                        : filtered.OrderByDescending(f => f.FullName),
                    _ => _sortOrder == "ASC"
                        ? filtered.OrderBy(f => f.AverageRating)
                        : filtered.OrderByDescending(f => f.AverageRating)
                };

                // Обновляем UI
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Friends.Clear();
                    foreach (var friend in filtered)
                    {
                        Friends.Add(friend);
                    }
                });
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки каталога: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCitiesAsync(List<string> cities)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableCities.Clear();
                AvailableCities.Add("Все города");

                foreach (var city in cities)
                {
                    AvailableCities.Add(city);
                }
            });
        }

        private async Task ApplyFiltersAsync()
        {
            await LoadItemsAsync();
        }

        private async Task ResetFiltersAsync()
        {
            SelectedCity = "Все города";
            MaxHourlyRate = null;
            OnlyVerified = true;
            MinRating = null;
            _sortBy = "Rating";
            _sortOrder = "DESC";

            await LoadItemsAsync();

            Base.Messenger.Default.SendNotification("Фильтры сброшены");
        }

        private Task ViewFriendDetails(FPInfoDTO friend)
        {
            if (friend != null)
            {
                Base.Messenger.Default.SendData(new { FriendProfileID = friend.ProfileID });
            }
            return Task.CompletedTask;
        }

        private async Task SortByAsync(string sortField)
        {
            if (_sortBy == sortField)
            {
                _sortOrder = _sortOrder == "ASC" ? "DESC" : "ASC";
            }
            else
            {
                _sortBy = sortField;
                _sortOrder = "ASC";
            }

            await LoadItemsAsync();
        }

        // Вспомогательные методы для UI
        public string FormatHourlyRate(decimal? rate)
        {
            return rate.HasValue ? $"{rate.Value:F0} ₽/час" : "Цена не указана";
        }

        public string FormatRating(decimal? rating)
        {
            return rating.HasValue ? $"{rating.Value:F1} ★" : "Нет оценок";
        }

        public string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "??";

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
    }
}