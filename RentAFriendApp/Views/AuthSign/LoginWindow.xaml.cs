using RentAFriendApp.Models;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace RentAFriendApp.Views.AuthSign
{
    public partial class LoginWindow : Window
    {
        private bool _isClosing = false;

        public LoginWindow()
        {
            InitializeComponent();

            var loginViewModel = new ViewModels.AuthSign.LoginViewModel(
                OnLoginSuccess,
                ShowRegisterWindow
            );

            DataContext = loginViewModel;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            Loaded += (s, e) => EmailTextBox.Focus();
        }

        private void CloseWithAnimation()
        {
            if (_isClosing) return;

            _isClosing = true;

            var anim = new DoubleAnimation(0, TimeSpan.FromSeconds(0.3));
            anim.Completed += (s, e) =>
            {
                Dispatcher.Invoke(() => base.Close());
            };

            this.BeginAnimation(OpacityProperty, anim);
        }
        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-z]+\.[a-z]{2,10}$";

            bool isValid = Regex.IsMatch(email, pattern);
            if (!isValid)
            {
                EmailTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.Red);
            }
            else
            {
                EmailTextBox.ClearValue(TextBox.BorderBrushProperty);
            }

            return isValid;
        }
        private void OnLoginSuccess(Auth authData)
        {
            if (_isClosing || !IsLoaded)
                return;

            Dispatcher.Invoke(() =>
            {
                var mainWindow = new MainWindow(authData);
                mainWindow.Show();
                CloseWithAnimation();
            });
        }

        private void ShowRegisterWindow()
        {
            if (_isClosing || !IsLoaded)
                return;

            Dispatcher.Invoke(() =>
            {
                var registerWindow = new RegisterWindow();
                registerWindow.Show();
                CloseWithAnimation();
            });
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                if (viewModel.LoginCommand.CanExecute(null))
                {
                    viewModel.LoginCommand.Execute(null);
                }
            }
        }

        private void ForgotPasswordText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                viewModel?.ForgotPasswordCommand?.Execute(null);
            }
        }

        private void CreateAccountLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                viewModel?.RegisterCommand?.Execute(null);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseWithAnimation();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                CloseWithAnimation();
            }
            base.OnClosing(e);
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.AuthSign.LoginViewModel viewModel)
            {
                ValidateEmail(viewModel.Email);
            }
        }
    }
}