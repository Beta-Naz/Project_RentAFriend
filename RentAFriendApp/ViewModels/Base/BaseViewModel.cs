using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RentAFriendApp.ViewModels.Base
{
    /// <summary> Базовый класс для всех ViewModel с поддержкой уведомлений об изменении свойств </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary> Метод для уведомления об изменении свойства </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary> Установка значения свойства с уведомлением об изменении </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Свойства для управления состоянием UI
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary> Очистка сообщений об ошибках </summary>
        public void ClearErrors()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary> Установка ошибки </summary>
        public void SetError(string message)
        {
            ErrorMessage = message;
            HasError = !string.IsNullOrEmpty(message);
        }

        /// <summary> Виртуальный метод для инициализации ViewModel </summary>
        public virtual void Initialize() { }

        /// <summary> Виртуальный метод для очистки ресурсов </summary>
        public virtual void Cleanup() { }
    }
}
