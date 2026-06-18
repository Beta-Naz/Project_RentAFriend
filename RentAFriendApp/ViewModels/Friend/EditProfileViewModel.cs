using RentAFriendApp.Classes;
using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.FriendProfileDTO;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class EditProfileViewModel : BaseViewModel
    {
        private readonly string _token;

        private string _originalFullName;
        private string _originalEmail;
        private string _originalPhone = string.Empty;
        private string _originalBio;
        private int? _originalAge;
        private string _originalCity;
        private decimal? _originalHourlyRate;
        private string _originalHobbies;

        // Основная информация
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (value.Length > 115) return;
                if (SetProperty(ref _fullName, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(FullNameValidation));
                    OnPropertyChanged(nameof(SaveButtonText));
                    OnPropertyChanged(nameof(Initials));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if(value.Length > 100) return;
                if (SetProperty(ref _email, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(EmailValidation));
                    OnPropertyChanged(nameof(EmailValidationMessage));
                    OnPropertyChanged(nameof(IsEmailValid));
                    OnPropertyChanged(nameof(SaveButtonText));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        
        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set
            {
                if (SetProperty(ref _phone, value))
                {
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(PhoneValidation));
                    OnPropertyChanged(nameof(SaveButtonText));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Профиль друга
        private string _bio;
        public string Bio
        {
            get => _bio;
            set
            {
                if (value.Length > 2100) return;
                if (SetProperty(ref _bio, value))
                {
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(BioLength));
                    OnPropertyChanged(nameof(BioProgressColor));
                    OnPropertyChanged(nameof(BioProgressPercent));
                    OnPropertyChanged(nameof(BioValidation));
                    OnPropertyChanged(nameof(SaveButtonText));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _ageText;
        public string AgeText
        {
            get => _ageText;
            set
            {
                if (SetProperty(ref _ageText, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        Age = null;
                    }
                    else if (int.TryParse(value, out int result))
                    {
                        Age = result;
                    }
                    else
                    {
                        Age = null;
                    }

                    OnPropertyChanged(nameof(AgeDisplay));
                    OnPropertyChanged(nameof(AgeValidation));
                    OnPropertyChanged(nameof(IsFormValid));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(SaveButtonText));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private int? _age;
        public int? Age
        {
            get => _age;
            set
            {
                SetProperty(ref _age, value);
            }
        }

        private string _city;
        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value))
                {
                    OnPropertyChanged(nameof(IsFormValid));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(CityValidation));
                    OnPropertyChanged(nameof(SaveButtonText));
                    FilterCities(value);
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _hobbies;
        public string Hobbies
        {
            get => _hobbies;
            set
            {
                SetProperty(ref _hobbies, value);
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }

        private string _hourlyRateText;
        public string HourlyRateText
        {
            get => _hourlyRateText;
            set
            {
                if (SetProperty(ref _hourlyRateText, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        HourlyRate = null;
                    }
                    else
                    {
                        string normalizedValue = value.Replace(',', '.');
                        if (decimal.TryParse(normalizedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result))
                        {
                            HourlyRate = Math.Round(result, 2);
                        }
                        else
                        {
                            HourlyRate = null;
                        }
                    }
                    OnPropertyChanged(nameof(HourlyRateDisplay));
                    OnPropertyChanged(nameof(HourlyRateValidation));
                    OnPropertyChanged(nameof(IsFormValid));
                    OnPropertyChanged(nameof(HasChanges));
                    OnPropertyChanged(nameof(SaveButtonText));
                    ValidationStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private decimal? _hourlyRate;
        public decimal? HourlyRate
        {
            get => _hourlyRate;
            set => SetProperty(ref _hourlyRate, value);
        }

        // Статус верификации (только для просмотра)
        private bool _isVerified;
        public bool IsVerified
        {
            get => _isVerified;
            set => SetProperty(ref _isVerified, value);
        }

        // Доступные города для автодополнения
        private ObservableCollection<string> _availableCities;
        public ObservableCollection<string> AvailableCities
        {
            get => _availableCities;
            set => SetProperty(ref _availableCities, value);
        }

        // Отфильтрованные города
        private ObservableCollection<string> _filteredCities;
        public ObservableCollection<string> FilteredCities
        {
            get => _filteredCities;
            set => SetProperty(ref _filteredCities, value);
        }

        // Список хобби
        private ObservableCollection<string> _hobbiesList;
        public ObservableCollection<string> HobbiesList
        {
            get => _hobbiesList;
            set => SetProperty(ref _hobbiesList, value);
        }

        private string _newHobby;
        public string NewHobby
        {
            get => _newHobby;
            set
            {
                if (SetProperty(ref _newHobby, value))
                {
                    OnPropertyChanged(nameof(CanAddHobby));
                }
            }
        }

        // Команды
        public ICommand SaveProfileCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddHobbyCommand { get; }
        public ICommand RemoveHobbyCommand { get; }
        public ICommand SelectCityCommand { get; }
        public ICommand LoadDataCommand { get; }
        public ICommand ResetFormCommand { get; }
        public ICommand LostFocusPhoneCommand { get; }

        // События
        public event EventHandler? ProfileSaved;
        public event EventHandler? ProfileCanceled;
        public event EventHandler<DataLoadEventArgs>? DataLoaded;
        public event EventHandler? ValidationStateChanged;

        // Вычисляемые свойства
        public string FullNameValidation => ValidationHelper.ValidationFullName(FullName);
        public string EmailValidation => string.IsNullOrWhiteSpace(Email) ? "⚠ Обязательное поле" :
                                       !IsValidEmail(Email) ? "⚠ Неверный формат" : "✓";
        public string PhoneValidation => ValidationHelper.ValidPhoneText(Phone);

        public string CityValidation => string.IsNullOrWhiteSpace(City) ? "⚠ Обязательное поле" :
                                       City.Length < 2 ? "⚠ Минимум 2 символа" : "✓";
        public string AgeValidation => Age.HasValue ? (Age < 18 || Age > 100) ? "⚠ От 18 до 100 лет" : "✓" : "⚠ Обязательное поле";
        public string HourlyRateValidation => ValidationHelper.HourlyRateTextValidation(HourlyRateText, HourlyRate);
        public string ChangesCountDisplay => GetChangesCount() > 0 ? $"{GetChangesCount()} изменений" : "Нет изменений";
        public string HobbiesValidation => HobbiesList.Count > 20 ? "⚠ Максимум 20 хобби" : "✓";
        public string VerificationIcon => IsVerified ? "✓" : "⚠";
        public string VerificationStatus => IsVerified ? "Верифицирован" : "Не верифицирован";
        public System.Windows.Media.Brush VerificationStatusColor => IsVerified
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
        public string BioValidation => BioLength > 2000 ? "⚠ Максимум 2000 символов" : "✓";
        public string EmailValidationMessage => IsValidEmail(Email) ? "✓ Корректный email" : "⚠ Неверный формат email";
        public bool IsEmailValid => IsValidEmail(Email);
        public bool CanAddHobby => !string.IsNullOrWhiteSpace(NewHobby) && NewHobby.Length >= 2;
        public bool IsFormValid => CanSaveProfile();
        public bool HasChanges => CheckForChanges();
        public string SaveButtonText => IsBusy && HasChanges ? "Сохранение..." : HasChanges ? "Сохранить изменения" : "Нет изменений";
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "??";
                var parts = FullName.Trim().Split(' ');
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                return FullName.Length >= 2 ? FullName.Substring(0, 2).ToUpper() : FullName.ToUpper();
            }
        }
        public int BioLength => Bio?.Length ?? 0;
        public string BioProgressColor => BioLength > 1900 ? "#F44336" : BioLength > 1500 ? "#FF9800" : "#4CAF50";
        public int BioProgressPercent => BioLength * 100 / 2000;
        public string HourlyRateDisplay => HourlyRate.HasValue ? $"{HourlyRate.Value:N0} ₽/час" : "Не указано";
        public string AgeDisplay => Age.HasValue && Age.Value >= 18 && Age.Value <= 100 ? $"{Age.Value} лет" : "Не указан";

        public EditProfileViewModel(string token)
        {
            _token = token;
            Title = "Редактирование профиля";

            AvailableCities = new ObservableCollection<string>();
            FilteredCities = new ObservableCollection<string>();
            HobbiesList = new ObservableCollection<string>();

            SaveProfileCommand = new RelayCommandAsync(SaveProfileAsync, () => CanSaveProfile() && HasChanges && !IsBusy);
            CancelCommand = new RelayCommandAsync(OnCancel);
            AddHobbyCommand = new RelayCommandAsync(AddHobby, () => CanAddHobby);
            RemoveHobbyCommand = new RelayCommandAsync<string>(RemoveHobby);
            SelectCityCommand = new RelayCommandAsync<string>(SelectCity);
            LoadDataCommand = new RelayCommandAsync(LoadProfileDataAsync);
            ResetFormCommand = new RelayCommandAsync(ResetForm, () => HasChanges);
            LostFocusPhoneCommand = new RelayCommandAsync(FormatPhone);

            _ = LoadProfileDataAsync();
            _ = LoadAvailableCitiesAsync();
        }
        private Task FormatPhone()
        {
            if (ValidationHelper.IsValidRegexPhone(Phone) && Phone[0] == '8')
            {
                string digits = Regex.Replace(Phone, @"[^\d]", "");
                Phone = $"+7 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
            }
            return Task.CompletedTask;
        }
        private async Task LoadProfileDataAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Получаем данные пользователя
                var getUser = await UserContext.GetUser(_token);
                var user = getUser?.Data;
                if (user == null)
                {
                    SetError("Пользователь не найден");
                    return;
                }

                FullName = user.FullName;
                Phone = user.Phone ?? string.Empty;
                Email = user.Email;

                var myProfileResponse = await FriendProfileContext.GetMyProfile(_token);
                var profile = myProfileResponse?.Profile;

                if (profile != null)
                {
                    Bio = profile.Bio ?? string.Empty;
                    AgeText = profile.Age.ToString() ?? string.Empty;
                    City = profile.City ?? string.Empty;
                    Hobbies = profile.Hobbies ?? string.Empty;
                    HourlyRateText = profile.HourlyRate.ToString() ?? string.Empty;
                    IsVerified = profile.IsVerified;
                }
                FilteredCities.Clear();
                // Сохраняем оригинальные значения
                UpdateOriginalValues();
                // Загрузка хобби в список
                LoadHobbiesToList();

                OnPropertyChanged(nameof(HasChanges));
                OnPropertyChanged(nameof(Initials));
                OnPropertyChanged(nameof(SaveButtonText));
                OnPropertyChanged(nameof(VerificationStatus));
                OnPropertyChanged(nameof(VerificationStatusColor));
                OnPropertyChanged(nameof(VerificationIcon));
                DataLoaded?.Invoke(this, new DataLoadEventArgs { Success = true });
                
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки профиля: {ex.Message}");
                DataLoaded?.Invoke(this, new DataLoadEventArgs { Success = false, ErrorMessage = ex.Message });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAvailableCitiesAsync()
        {
            try
            {
                var cities = await FriendProfileContext.GetAvailableCities(_token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableCities.Clear();
                    if (cities != null)
                    {
                        foreach (var city in cities)
                        {
                            AvailableCities.Add(city);
                        }
                    }

                    // Добавляем популярные города
                    var popularCities = new[]
                    {
                        "Москва", "Пермь", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань",
                        "Нижний Новгород", "Челябинск", "Самара", "Омск", "Ростов-на-Дону"
                    };

                    foreach (var city in popularCities)
                    {
                        if (!AvailableCities.Contains(city))
                            AvailableCities.Add(city);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки городов: {ex.Message}");
            }
        }

        private bool CanSaveProfile()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   FullName.Length >= 2 &&
                   FullName.Length <= 100 &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   IsValidEmail(Email) &&
                   Email.Length <= 100 &&
                   !string.IsNullOrWhiteSpace(City) &&
                   City.Length >= 2 &&
                   City.Length <= 100 &&
                   HourlyRate.HasValue &&
                   Age.HasValue &&
                   Age >= 18 &&
                   Age <= 100 &&
                   HourlyRate.Value > 0 &&
                   HourlyRate.Value <= 10000 &&
                   BioLength <= 2000;
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                UpdateHobbiesFromList();

                // Обновляем пользователя
                var userUpdate = new UserMainInfoDTO
                {
                    FullName = FullName,
                    Phone = Phone
                };
                await UserContext.UpdateUser(_token, userUpdate);

                // Обновляем профиль друга
                var profileUpdate = new FPMainInfoDTO
                {
                    Bio = Bio,
                    Age = Age,
                    City = City,
                    Hobbies = Hobbies,
                    HourlyRate = HourlyRate
                };
                string message = "Ошибка сохранения профиля, попробуйте позже";
                var getProfile = await FriendProfileContext.GetMyProfile(_token);
                if (getProfile != null)
                {
                    if (getProfile.Ok)
                    {
                        var profile = await FriendProfileContext.UpdateProfile(_token, profileUpdate);
                        message = profile?.Message ?? message;
                    }
                    else
                    {
                        var profile = await FriendProfileContext.CreateProfile(_token, profileUpdate);
                        message = profile?.Message ?? message;
                    }
                }
                Messenger.Default.SendNotification(message);

                UpdateOriginalValues();
                OnPropertyChanged(nameof(HasChanges));
                
                CommandManager.InvalidateRequerySuggested();
                

                ProfileSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetError($"❌ Ошибка сохранения профиля: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnCancel()
        {
            if (HasChanges)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения. Вы уверены, что хотите отменить?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            ProfileCanceled?.Invoke(this, EventArgs.Empty);
            Base.Messenger.Default.SendNotification("Редактирование отменено");
            await Task.CompletedTask;
        }

        private async Task ResetForm()
        {
            FullName = _originalFullName;
            Email = _originalEmail;
            Phone = _originalPhone ?? string.Empty;
            Bio = _originalBio ?? string.Empty;
            Age = _originalAge;
            City = _originalCity ?? string.Empty;
            HourlyRate = _originalHourlyRate;
            Hobbies = _originalHobbies ?? string.Empty;

            LoadHobbiesToList();

            OnPropertyChanged(nameof(HasChanges));
            
            OnPropertyChanged(nameof(Initials));

            Base.Messenger.Default.SendNotification("Форма сброшена к исходным значениям");
            await Task.CompletedTask;
        }

        private async Task AddHobby()
        {
            if (!string.IsNullOrWhiteSpace(NewHobby) &&
                !HobbiesList.Contains(NewHobby.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                HobbiesList.Add(NewHobby.Trim());
                NewHobby = string.Empty;
                OnPropertyChanged(nameof(HasChanges));
                OnPropertyChanged(nameof(ChangesCountDisplay));
                ValidationStateChanged?.Invoke(this, EventArgs.Empty);
            }
            OnPropertyChanged(nameof(SaveButtonText));
            await Task.CompletedTask;
        }

        private async Task RemoveHobby(string? hobby)
        {
            if (!string.IsNullOrEmpty(hobby))
            {
                HobbiesList.Remove(hobby);
                OnPropertyChanged(nameof(HasChanges));
                OnPropertyChanged(nameof(ChangesCountDisplay));
                ValidationStateChanged?.Invoke(this, EventArgs.Empty);
            }
            OnPropertyChanged(nameof(SaveButtonText));
            await Task.CompletedTask;
        }

        private async Task SelectCity(string? city)
        {
            if (!string.IsNullOrEmpty(city))
            {
                City = city;
                FilteredCities.Clear();
            }
            await Task.CompletedTask;
        }

        private void FilterCities(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredCities.Clear();
                return;
            }

            var filtered = AvailableCities
                .Where(c => c.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(5)
                .ToList();

            FilteredCities.Clear();
            foreach (var city in filtered)
            {
                FilteredCities.Add(city);
            }
        }

        private void LoadHobbiesToList()
        {
            HobbiesList.Clear();
            if (!string.IsNullOrEmpty(Hobbies))
            {
                var hobbiesArray = Hobbies.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var hobby in hobbiesArray)
                {
                    HobbiesList.Add(hobby.Trim());
                }
            }
        }

        private void UpdateHobbiesFromList()
        {
            if (HobbiesList.Count > 0)
            {
                Hobbies = string.Join(", ", HobbiesList);
            }
            else
            {
                Hobbies = string.Empty;
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-z]+\.[a-z]{2,10}$";

            bool isValid = Regex.IsMatch(email, pattern);
            return isValid;
        }

        private bool CheckForChanges()
        {
            
            OnPropertyChanged(nameof(ChangesCountDisplay));
            return FullName != _originalFullName ||
                   Email != _originalEmail ||
                   Phone != _originalPhone ||
                   Bio != _originalBio ||
                   Age != _originalAge ||
                   City != _originalCity ||
                   HourlyRate != _originalHourlyRate ||
                   !HobbiesListsEqual();
        }

        private bool HobbiesListsEqual()
        {
            var originalHobbies = _originalHobbies?.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .ToList() ?? new List<string>();

            return HobbiesList.SequenceEqual(originalHobbies, StringComparer.OrdinalIgnoreCase);
        }

        private int GetChangesCount()
        {
            int count = 0;
            if (FullName != _originalFullName) count++;
            if (Email != _originalEmail) count++;
            if (Phone != _originalPhone) count++;
            if (Bio != _originalBio) count++;
            if (Age != _originalAge) count++;
            if (City != _originalCity) count++;
            if (HourlyRate != _originalHourlyRate) count++;
            if (!HobbiesListsEqual()) count++;
            return count;
        }

        private void UpdateOriginalValues()
        {
            _originalFullName = FullName;
            _originalEmail = Email;
            _originalPhone = Phone;
            _originalBio = Bio;
            _originalAge = Age;
            _originalCity = City;
            _originalHourlyRate = HourlyRate;
            _originalHobbies = Hobbies;
        }

        // Вспомогательный класс для события загрузки данных
        public class DataLoadEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public DateTime LoadTime { get; set; } = DateTime.Now;
        }
    }
}