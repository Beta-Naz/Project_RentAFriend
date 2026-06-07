using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RentAFriendApp.Views.AuthSign
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();

            // Установка DataContext
            var registerViewModel = new ViewModels.AuthSign.RegisterViewModel(ShowLoginWindow, OnRegistrationSuccess);

            DataContext = registerViewModel;

            // Привязка PasswordBox к ViewModel
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;

            // Настройка валидации
            SetupValidation();

            // Фокус на выборе роли при загрузке
            Loaded += (s, e) => ClientRoleButton.Focus();
        }

        private void ShowLoginWindow()
        {
            // Возврат к окну входа
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void OnRegistrationSuccess()
        {
            // Успешная регистрация - открываем окно входа
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Показываем сообщение об успехе
            MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти в систему.",
                "Успешная регистрация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            this.Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;

                // Валидация пароля в реальном времени
                ValidatePasswordStrength(PasswordBox.Password);
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;

                // Проверка совпадения паролей в реальном времени
                ValidatePasswordMatch();
            }
        }

        private void RoleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string role)
            {
                // Снимаем выделение с другой кнопки
                if (role == "Client")
                {
                    FriendRoleButton.IsChecked = false;
                }
                else if (role == "Friend")
                {
                    ClientRoleButton.IsChecked = false;
                }

                if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
                {
                    viewModel.SelectRoleCommand.Execute(role);

                    // Обновляем заголовок формы на втором шаге
                    if (viewModel.CurrentStep == 2)
                    {
                        FormTitle.Text = $"Регистрация {(role == "Client" ? "клиента" : "друга")}";
                    }
                }

                // Показываем информацию о выбранной роли
                RoleInfoBorder.Visibility = Visibility.Visible;
                RoleTitle.Text = $"Вы выбрали: {(role == "Client" ? "Клиент" : "Друг")}";

                // Обновляем описание роли
                UpdateRoleDescription(role);

                // Активируем кнопку продолжения
                ContinueButton.IsEnabled = true;

                // Анимация появления информации
                RoleInfoBorder.Opacity = 0;
                RoleInfoBorder.BeginAnimation(OpacityProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(1,
                        TimeSpan.FromSeconds(0.3)));
            }
        }

        private void RoleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.IsChecked == false)
            {
                // Если обе кнопки не выбраны, скрываем информацию
                if (ClientRoleButton.IsChecked == false && FriendRoleButton.IsChecked == false)
                {
                    RoleInfoBorder.Visibility = Visibility.Collapsed;
                    ContinueButton.IsEnabled = false;

                    if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
                    {
                        viewModel.SelectedRole = null;
                    }
                }
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.ContinueToStep2Command.Execute(null);

                if (viewModel.CurrentStep == 2)
                {
                    // Переход ко второму шагу с анимацией
                    Step1RoleSelection.Visibility = Visibility.Collapsed;
                    Step2DataForm.Visibility = Visibility.Visible;

                    // Фокус на поле имени
                    FullNameTextBox.Focus();

                    // Обновление заголовка
                    FormTitle.Text = $"Регистрация {(viewModel.SelectedRole == "Client" ? "клиента" : "друга")}";

                    // Анимация появления формы
                    Step2DataForm.Opacity = 0;
                    Step2DataForm.BeginAnimation(OpacityProperty,
                        new System.Windows.Media.Animation.DoubleAnimation(1,
                            TimeSpan.FromSeconds(0.3)));
                    progressLine.Width = 200;
                    progressNumber.Text = "Шаг 2 из 2";
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.BackToStep1Command.Execute(null);

                // Возврат к первому шагу с анимацией
                Step2DataForm.Visibility = Visibility.Collapsed;
                Step1RoleSelection.Visibility = Visibility.Visible;

                // Анимация
                Step1RoleSelection.Opacity = 0;
                Step1RoleSelection.BeginAnimation(OpacityProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(1,
                        TimeSpan.FromSeconds(0.3)));
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                if (viewModel.RegisterCommand.CanExecute(null))
                {
                    viewModel.RegisterCommand.Execute(null);
                }
            }
            
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void Login_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.LoginCommand.Execute(null);
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                ValidateEmail(EmailTextBox.Text);
            }
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePhone(PhoneTextBox.Text);
        }

        // Автоматическое форматирование телефона
        private void PhoneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                var phone = Regex.Replace(PhoneTextBox.Text, @"[^\d]", "");
                if (phone.Length == 11)
                {
                    PhoneTextBox.Text = $"+7 ({phone.Substring(1, 3)}) {phone.Substring(4, 3)}-{phone.Substring(7, 2)}-{phone.Substring(9, 2)}";
                }
            }
        }
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender == FullNameTextBox)
                    EmailTextBox.Focus();
                else if (sender == EmailTextBox)
                    PhoneTextBox.Focus();
                else if (sender == PhoneTextBox)
                    PasswordBox.Focus();
                else if (sender == PasswordBox)
                    ConfirmPasswordBox.Focus();
                else if (sender == ConfirmPasswordBox)
                    AgreementCheckBox.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        // Настройка валидации
        private void SetupValidation()
        {
            // Стили для валидации
            var validStyle = new Style(typeof(TextBox));
            validStyle.Setters.Add(new Setter(Control.BorderBrushProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)));

            var invalidStyle = new Style(typeof(TextBox));
            invalidStyle.Setters.Add(new Setter(Control.BorderBrushProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)));

            // Создание триггеров валидации
            var emailTrigger = new DataTrigger
            {
                Binding = new System.Windows.Data.Binding("Email")
                {
                    Converter = new EmailValidationConverter()
                },
                Value = false
            };
            emailTrigger.Setters.Add(new Setter(Control.BorderBrushProperty,
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)));

            // Добавление стилей в ресурсы
            Resources.Add("ValidTextBoxStyle", validStyle);
            Resources.Add("InvalidTextBoxStyle", invalidStyle);
        }

        // Валидация email
        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Регулярное выражение для проверки email
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            bool isValid = Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);

            if (!isValid)
            {
                // Подсветка ошибки
                EmailTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.Red);
            }
            else
            {
                EmailTextBox.ClearValue(TextBox.BorderBrushProperty);
            }

            return isValid;
        }

        // Валидация телефона
        private bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                // Телефон не обязателен
                PhoneTextBox.ClearValue(TextBox.BorderBrushProperty);
                return true;
            }

            // Убираем все нецифровые символы
            string digits = Regex.Replace(phone, @"[^\d]", "");

            // Российские номера: +7 XXX XXX-XX-XX или 8 XXX XXX-XX-XX
            bool isValid = (digits.Length == 11 && (digits.StartsWith("7") || digits.StartsWith("8"))) ||
                          (digits.Length == 10);

            if (!isValid)
            {
                PhoneTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.Red);
            }
            else
            {
                PhoneTextBox.ClearValue(TextBox.BorderBrushProperty);
            }

            return isValid;
        }

        // Валидация силы пароля
        private void ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                PasswordBox.ToolTip = "Введите пароль";
                PasswordBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.LightGray);
                return;
            }

            // Проверка требований к паролю
            bool hasMinLength = password.Length >= 8;
            bool hasDigit = Regex.IsMatch(password, @"\d");
            bool hasLetter = Regex.IsMatch(password, @"[a-zA-Z]");
            bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]");

            int strength = 0;
            if (hasMinLength) strength++;
            if (hasDigit) strength++;
            if (hasLetter) strength++;
            if (hasSpecialChar) strength++;

            // Визуальная индикация силы пароля
            System.Windows.Media.Color color;
            string tooltip;
            if (!Regex.IsMatch(password, @"[а-яА-Я]"))
            {
                switch (strength)
                {
                    case 0:
                    case 1:
                        color = System.Windows.Media.Colors.Red;
                        tooltip = "Очень слабый пароль";
                        break;
                    case 2:
                        color = System.Windows.Media.Colors.Orange;
                        tooltip = "Слабый пароль";
                        break;
                    case 3:
                        color = System.Windows.Media.Colors.Yellow;
                        tooltip = "Средний пароль";
                        break;
                    case 4:
                        color = System.Windows.Media.Colors.Green;
                        tooltip = "Сильный пароль";
                        break;
                    default:
                        color = System.Windows.Media.Colors.Gray;
                        tooltip = "";
                        break;
                }
            }
            else
            {
                tooltip = "Используйте только латинские буквы";
                color = System.Windows.Media.Colors.Red;
            }
            PasswordBox.BorderBrush = new System.Windows.Media.SolidColorBrush(color);
            PasswordBox.ToolTip = tooltip;

            // Показываем индикатор силы пароля
            ShowPasswordStrengthIndicator(strength);
        }

        // Показ индикатора силы пароля
        private void ShowPasswordStrengthIndicator(int strength)
        {
            // Создаем индикатор (можно заменить на ProgressBar или другой контрол)
            var strengthGrid = new Grid
            {
                Height = 4,
                Margin = new Thickness(0, 5, 0, 0)
            };

            strengthGrid.ColumnDefinitions.Add(new ColumnDefinition());
            strengthGrid.ColumnDefinitions.Add(new ColumnDefinition());
            strengthGrid.ColumnDefinitions.Add(new ColumnDefinition());
            strengthGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < 4; i++)
            {
                var border = new Border
                {
                    Background = i < strength ?
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) :
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(2, 0, 2, 0)
                };

                Grid.SetColumn(border, i);
                strengthGrid.Children.Add(border);
            }

            // Добавляем индикатор под полем пароля
            var stackPanel = PasswordBox.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Children.Count > 1)
            {
                // Удаляем старый индикатор
                if (stackPanel.Children[1] is Grid)
                {
                    stackPanel.Children.RemoveAt(1);
                }

                // Добавляем новый индикатор
                stackPanel.Children.Insert(1, strengthGrid);
            }
        }

        // Проверка совпадения паролей
        private void ValidatePasswordMatch()
        {
            if (string.IsNullOrEmpty(PasswordBox.Password) ||
                string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                ConfirmPasswordBox.ClearValue(PasswordBox.BorderBrushProperty);
                return;
            }

            bool passwordsMatch = PasswordBox.Password == ConfirmPasswordBox.Password;

            if (!passwordsMatch)
            {
                ConfirmPasswordBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.Red);
                ConfirmPasswordBox.ToolTip = "Пароли не совпадают";

                // Показываем сообщение об ошибке
                ShowErrorMessage("Пароли не совпадают");
            }
            else
            {
                ConfirmPasswordBox.ClearValue(PasswordBox.BorderBrushProperty);
                ConfirmPasswordBox.ToolTip = "Пароли совпадают";
                HideErrorMessage();
            }
        }

        // Обновление описания роли
        private void UpdateRoleDescription(string role)
        {
            if (role == "Client")
            {
                RoleDescription.Text = "Как клиент вы сможете искать и бронировать встречи с друзьями, " +
                                      "оставлять отзывы и накапливать репутацию.";
            }
            else if (role == "Friend")
            {
                RoleDescription.Text = "Как друг вы сможете предлагать свои услуги компаньона, " +
                                      "устанавливать расписание, назначать цены и получать заказы.";
            }
        }

        // Показать сообщение об ошибке
        private void ShowErrorMessage(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;

            // Анимация появления
            ErrorBorder.Opacity = 0;
            ErrorBorder.BeginAnimation(OpacityProperty,
                new System.Windows.Media.Animation.DoubleAnimation(1,
                    TimeSpan.FromSeconds(0.3)));
        }

        // Скрыть сообщение об ошибке
        private void HideErrorMessage()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        // Конвертер для валидации email (дополнительный класс)
        public class EmailValidationConverter : System.Windows.Data.IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                if (value is string email)
                {
                    try
                    {
                        var addr = new System.Net.Mail.MailAddress(email);
                        return addr.Address == email;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private void FullNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void AgreementCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.RegisterViewModel viewModel)
            {
                viewModel.AgreeToTerms = (bool)AgreementCheckBox.IsChecked;
            }
        }
    }
}