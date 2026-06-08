namespace RentAFriendApp.ViewModels.Base
{
    /// <summary> Класс для передачи сообщений между ViewModel </summary>
    public class Messenger
    {
        public static Messenger? Instanse;
        private static readonly object _lock = new object();

        public static Messenger Default
        {
            get
            {
                if (Instanse == null)
                {
                    lock (_lock)
                    {
                        if (Instanse == null)
                        {
                            Instanse = new Messenger();
                        }
                    }
                }
                return Instanse;
            }
        }

        public event EventHandler<string> NotificationReceived;
        public event EventHandler<object> DataReceived;
        public event EventHandler<bool> BusyStateChanged;

        private Messenger() { }

        /// <summary> Отправка текстового уведомления </summary>
        public void SendNotification(string message)
        {
            NotificationReceived?.Invoke(this, message);
        }

        /// <summary> Отправка данных </summary>
        public void SendData(object data)
        {
            DataReceived?.Invoke(this, data);
        }

        /// <summary> Изменение состояния занятости </summary>
        public void SetBusy(bool isBusy)
        {
            BusyStateChanged?.Invoke(this, isBusy);
        }

        /// <summary> Отправка сообщения определенного типа </summary>
        public void Send<T>(T message) where T : class
        {
            DataReceived?.Invoke(this, message);
        }
    }
}