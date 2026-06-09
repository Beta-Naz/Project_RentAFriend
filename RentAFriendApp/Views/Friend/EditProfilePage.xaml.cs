using RentAFriendApp.ViewModels.Base;
using RentAFriendApp.ViewModels.Friend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RentAFriendApp.Views.Friend
{
    /// <summary>
    /// Логика взаимодействия для EditProfilePage.xaml
    /// </summary>
    public partial class EditProfilePage : Page
    {
        private EditProfileViewModel _viewModel;

        public EditProfilePage(string token)
        {
            InitializeComponent();
            _viewModel = new EditProfileViewModel(token);
            this.DataContext = _viewModel;

            // Подписываемся на события
            _viewModel.ProfileSaved += OnProfileSaved;
            _viewModel.ProfileCanceled += OnProfileCanceled;
            _viewModel.DataLoaded += OnDataLoaded;
            _viewModel.ValidationStateChanged += OnValidationStateChanged;

            // Подписываемся на системные уведомления
            Messenger.Default.NotificationReceived += OnNotificationReceived;
            Messenger.Default.DataReceived += OnDataReceived;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем данные если еще не загружены
            if (_viewModel != null)
            {
                // Можно добавить задержку для плавности
                System.Threading.Tasks.Task.Delay(300).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Проверяем, нужно ли обновить данные
                        if (string.IsNullOrEmpty(_viewModel.FullName))
                        {
                            _viewModel.LoadDataCommand.Execute(null);
                        }
                    });
                });
            }
        }

        private void OnProfileSaved(object sender, EventArgs e)
        {
            // Анимация успешного сохранения
            ShowSuccessAnimation();

            // Можно добавить навигацию назад или другие действия
            Dispatcher.Invoke(() =>
            {
                // Показываем сообщение
                MessageBox.Show("Профиль успешно сохранен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnProfileCanceled(object sender, EventArgs e)
        {
            // Навигация назад или закрытие страницы
            Dispatcher.Invoke(() =>
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            });
        }

        private void OnDataLoaded(object sender, EditProfileViewModel.DataLoadEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    // Показываем успешную загрузку
                    ShowToast("Данные профиля загружены успешно", "#4CAF50");
                }
                else
                {
                    // Показываем ошибку
                    ShowToast($"Ошибка загрузки: {e.ErrorMessage}", "#F44336");
                }
            });
        }

        private void OnValidationStateChanged(object sender, EventArgs e)
        {
            // Можно добавить анимацию изменения состояния валидации
            Dispatcher.Invoke(() =>
            {
                UpdateValidationVisuals();
            });
        }

        private void OnNotificationReceived(object sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ShowToast(message, "#2196F3");
            });
        }

        private void OnDataReceived(object sender, object data)
        {
            if (data != null)
            {
                var type = data.GetType();
                var actionProperty = type.GetProperty("Action");
                if (actionProperty != null)
                {
                    var actionValue = actionProperty.GetValue(data) as string;
                    if (actionValue == "ProfileUpdated")
                    {
                        _viewModel.LoadDataCommand.Execute(null);
                    }
                }
            }
        }

        private void ShowSuccessAnimation()
        {
            // Анимация успеха
            var storyboard = new Storyboard();

            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = true
            };

            Storyboard.SetTarget(scaleAnimation, this);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

            storyboard.Children.Add(scaleAnimation);
            storyboard.Begin();
        }

        private void ShowToast(string message, string color)
        {
            // Создаем временное уведомление
            var toast = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            toast.Child = textBlock;

            // Добавляем в Grid
            var grid = this.Content as Grid;
            if (grid != null)
            {
                grid.Children.Add(toast);

                // Анимация появления и исчезновения
                var storyboard = new Storyboard();

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    BeginTime = TimeSpan.FromSeconds(2)
                };

                Storyboard.SetTarget(fadeIn, toast);
                Storyboard.SetTarget(fadeOut, toast);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));

                storyboard.Children.Add(fadeIn);
                storyboard.Children.Add(fadeOut);

                storyboard.Completed += (s, e) => grid.Children.Remove(toast);
                storyboard.Begin();
            }
        }

        private void UpdateValidationVisuals()
        {
            // Здесь можно добавить дополнительные визуальные эффекты валидации
            // Например, подсветку полей, анимации и т.д.
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Анимация при фокусе
                var animation = new DoubleAnimation
                {
                    To = 1.02,
                    Duration = TimeSpan.FromSeconds(0.1)
                };

                textBox.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                textBox.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Возвращаем размер
                var animation = new DoubleAnimation
                {
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.1)
                };

                textBox.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                textBox.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }
    }
}