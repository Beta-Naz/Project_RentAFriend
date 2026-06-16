using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.UserDTO;
using RentAFriendApp.ViewModels.Base;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.AuthSign
{
    internal class RegisterViewModel : BaseViewModel
    {
        private readonly Action _showLoginWindow;
        private readonly Action _showLoginWindowCompliteRegister;

        private string _selectedRole = string.Empty;
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    UpdateRoleDescription();
                    OnPropertyChanged(nameof(IsRoleSelected));
                    OnPropertyChanged(nameof(Step1Title));
                }
            }
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private bool _agreeToTerms = false;
        public bool AgreeToTerms
        {
            get => _agreeToTerms;
            set
            {
                SetProperty(ref _agreeToTerms, value);
                CanRegister();
            }
        }

        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        private string _roleDescription = string.Empty;
        public string RoleDescription
        {
            get => _roleDescription;
            set => SetProperty(ref _roleDescription, value);
        }

        // Вычисляемые свойства
        public bool IsRoleSelected => !string.IsNullOrEmpty(SelectedRole);
        public string Step1Title => $"Выберите вашу роль ({(IsRoleSelected ? "Выбрано" : "Не выбрано")})";
        public string Step2Title => $"Регистрация {(SelectedRole == "Client" ? "клиента" : "друга")}";

        // Команды
        public ICommand? SelectRoleCommand { get; }
        public ICommand? ContinueToStep2Command { get; }
        public ICommand? BackToStep1Command { get; }
        public ICommand? RegisterCommand { get; }
        public ICommand? LoginCommand { get; }
        public ICommand? CloseCommand { get; }

        public RegisterViewModel(Action showLoginWindow, Action showLoginWindowCompliteRegister)
        {
            _showLoginWindow = showLoginWindow;
            _showLoginWindowCompliteRegister = showLoginWindowCompliteRegister;

            Title = "Регистрация";

            SelectRoleCommand = new RelayCommandAsync<string>(SelectRole);
            ContinueToStep2Command = new RelayCommandAsync(ContinueToStep2, CanContinueToStep2);
            BackToStep1Command = new RelayCommandAsync(BackToStep1);
            RegisterCommand = new RelayCommandAsync(RegisterAsync, CanRegister);
            LoginCommand = new RelayCommandAsync(Login);
            CloseCommand = new RelayCommandAsync(Close);
        }

        private Task SelectRole(string role)
        {
            SelectedRole = role;
            return Task.CompletedTask;
        }

        private bool CanContinueToStep2()
        {
            return IsRoleSelected && !IsBusy;
        }

        private Task ContinueToStep2()
        {
            if (IsRoleSelected)
            {
                CurrentStep = 2;
                OnPropertyChanged(nameof(Step2Title));
            }
            return Task.CompletedTask;
        }

        private Task BackToStep1()
        {
            CurrentStep = 1;
            ClearFormData();
            return Task.CompletedTask;
        }

        public bool CanRegister()
        {
            ClearErrors();
            bool canRegister = false;
            if(CurrentStep != 2)
            {
                SetError("Как?");
            }
            else if (string.IsNullOrWhiteSpace(FullName))
            {
                SetError("Полное имя не должно быть пустым");
            }
            else if (string.IsNullOrWhiteSpace(Email))
            {
                SetError("Почта не может быть пустой");
            }
            if (!IsValidEmail(Email.Trim()))
            {
                SetError("Формат почты неправильный");
            }
            else if (string.IsNullOrWhiteSpace(Password))
            {
                SetError("Пароль не может быть пустым");
            }
            else if (Password != ConfirmPassword)
            {
                SetError("Пароли не совпадают");
            }
            else if (!AgreeToTerms)
            {
                SetError("Необходимо согласиться с условиями использования");
            }
            else if (IsBusy) 
            {
                SetError("Попробуйте через несколько секунд");
            }
            else
            {
                canRegister = true;
            }
                return canRegister;
        }

        private async Task RegisterAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();
                bool isExistEmail = await UserContext.ExistsEmail(Email.Trim());
                if (isExistEmail)
                {
                    SetError("Аккаунт с такой электронной почтой уже существует");
                    return;
                }
                var registerDto = new UserRegisterDTO
                {
                    Email = Email.Trim(),
                    Password = Password,
                    FullName = FullName.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    Role = SelectedRole,
                    AgreeToTerms = AgreeToTerms
                };
                bool isRegister = await UserContext.Register(registerDto);
                if (isRegister)
                {
                    _showLoginWindowCompliteRegister?.Invoke();
                }
                else
                {
                    SetError("Ошибка регистрации");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка регистрации: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        private Task Login()
        {
            _showLoginWindow?.Invoke();
            return Task.CompletedTask;
        }
        private Task Close()
        {
            Application.Current.Shutdown();
            return Task.CompletedTask;
        }

        private void UpdateRoleDescription()
        {
            if (SelectedRole == "Client")
            {
                RoleDescription = "Как клиент вы сможете искать и бронировать встречи с друзьями, " +
                                 "оставлять отзывы и накапливать репутацию.";
            }
            else if (SelectedRole == "Friend")
            {
                RoleDescription = "Как друг вы сможете предлагать свои услуги компаньона, " +
                                 "устанавливать расписание, назначать цены и получать заказы.";
            }
            else
            {
                RoleDescription = "Выберите роль для продолжения регистрации";
            }
        }
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-z]+\.[a-z]{2,10}$";

            bool isValid = Regex.IsMatch(email, pattern);
            return isValid;
        }
        public bool IsClientRoleSelected
        {
            get => SelectedRole == "Client";
            set
            {
                if (value)
                {
                    SelectedRole = "Client";
                }
                else if (SelectedRole == "Client")
                {
                    SelectedRole = string.Empty;
                }
                OnPropertyChanged();
            }
        }

        public void ClearFormData()
        {
            FullName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            AgreeToTerms = false;
            ClearErrors();
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(Password));
            OnPropertyChanged(nameof(ConfirmPassword));
            OnPropertyChanged(nameof(AgreeToTerms));
        }
    }
}