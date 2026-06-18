using RentAFriendApp.Context;
using RentAFriendApp.Models;
using RentAFriendApp.ViewModels.Base;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.AuthSign
{
    internal class LoginViewModel : BaseViewModel
    {
        private readonly Action<Auth>? _onLoginSuccess;
        private readonly Action? _showRegisterWindow;

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        private bool _hasError = false;
        public new bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public ICommand? LoginCommand { get; }
        public ICommand? RegisterCommand { get; }
        public ICommand? ForgotPasswordCommand { get; }
        public ICommand? CloseCommand { get; }

        public LoginViewModel(Action<Auth> onLoginSuccess, Action showRegisterWindow)
        {
            _onLoginSuccess = onLoginSuccess;
            _showRegisterWindow = showRegisterWindow;

            Title = "Вход в систему";

            // Инициализация команд
            LoginCommand = new RelayCommandAsync(LoginAsync, CanLogin);
            RegisterCommand = new RelayCommandAsync(Register);
            ForgotPasswordCommand = new RelayCommandAsync(ForgotPassword);
            CloseCommand = new RelayCommandAsync(Close);
        }

        private bool CanLogin()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async Task LoginAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                // Валидация email
                if (!IsValidEmail(Email))
                {
                    SetError("Пожалуйста, введите корректный email");
                    if(Password == "Asdfg123")
                    {
                        SetError("Вы зашли как студент a-502, загрузка...");
                    }
                    IsBusy = false;
                    return;
                }

                // Проверяем, что пароль не пустой
                if (string.IsNullOrWhiteSpace(Password))
                {
                    SetError("Введите пароль");
                    IsBusy = false;
                    return;
                }

                var authData = await UserContext.Login(Email, Password);
                if (authData == null || string.IsNullOrEmpty(authData.Token))
                {
                    string error = authData?.Message ?? "Неверный email или пароль!";
                    SetError(error);
                    return;
                }
                if (string.IsNullOrEmpty(authData.Role))
                {
                    SetError("Ошибка получения данных пользователя");
                    return;
                }
                _onLoginSuccess?.Invoke(authData);
            }
            catch (Exception ex)
            {
                SetError($"Ошибка входа: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-z]+\.[a-z]{2,10}$";

            bool isValid = Regex.IsMatch(email, pattern);
            return isValid;
        }

        private Task Register()
        {
            _showRegisterWindow?.Invoke();
            return Task.CompletedTask;
        }

        private Task ForgotPassword()
        {
            MessageBox.Show("Функция восстановления пароля временно недоступна. \n Попробуйте ввести Asdfg123 или Qwerty123 ^-^",
                          "Восстановление пароля",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        private Task Close()
        {
            Application.Current.Shutdown();
            return Task.CompletedTask;
        }

        private new void SetError(string message)
        {
            ErrorMessage = message;
            HasError = !string.IsNullOrEmpty(message);
        }

        private new void ClearErrors()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}