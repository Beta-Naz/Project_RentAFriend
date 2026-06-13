using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Base
{
    /// <summary>
    /// Асинхронная команда для WPF (с поддержкой параметра)
    /// </summary>
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<Task>? _execute;
        private readonly Func<bool>? _canExecute;
        private readonly Func<object?, Task>? _executeWithParameter;
        private readonly Func<object?, bool>? _canExecuteWithParameter;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary> Конструктор для команды без параметра </summary>
        public RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary> Конструктор для команды с параметром object </summary>
        public RelayCommandAsync(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParameter = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (IsExecuting)
                return false;

            if (_canExecuteWithParameter != null)
                return _canExecuteWithParameter(parameter);

            if (_canExecute != null)
                return _canExecute();

            return true;
        }

        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Выполняет команду асинхронно и возвращает Task для await
        /// </summary>
        public async Task ExecuteAsync(object? parameter = null)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                IsExecuting = true;

                if (_executeWithParameter != null)
                    await _executeWithParameter(parameter);
                else if (_execute != null)
                    await _execute();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Асинхронная команда с типизированным параметром
    /// </summary>
    public class RelayCommandAsync<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommandAsync(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (IsExecuting)
                return false;

            if (parameter is T typedParam)
                return _canExecute?.Invoke(typedParam) ?? true;

            return _canExecute == null && parameter == null;
        }

        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Выполняет команду асинхронно и возвращает Task для await
        /// </summary>
        public async Task ExecuteAsync(object? parameter = default)
        {
            if (!CanExecute(parameter))
                return;

            if (parameter is not T typedParam && parameter != null)
                return;

            try
            {
                IsExecuting = true;
                await _execute((T?)parameter);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}