using System.Text.RegularExpressions;
using RentAFriendApp.ViewModels.Base;

namespace RentAFriendApp.ViewModels.Client
{
    internal class PaymentViewModel : BaseViewModel
    {
        public string Token { get; }
        public int BookingId { get; }

        #region Свойства

        private string _friendName = "";
        public string FriendName { get => _friendName; set => SetProperty(ref _friendName, value); }

        private string _dateTimeDisplay = "";
        public string DateTimeDisplay { get => _dateTimeDisplay; set => SetProperty(ref _dateTimeDisplay, value); }

        private string _durationDisplay = "";
        public string DurationDisplay { get => _durationDisplay; set => SetProperty(ref _durationDisplay, value); }

        private decimal _totalAmount;
        public decimal TotalAmount { get => _totalAmount; set => SetProperty(ref _totalAmount, value); }

        private string _cardNumber = "";
        public string CardNumber { get => _cardNumber; set { SetProperty(ref _cardNumber, value); OnPropertyChanged(nameof(IsFormValid)); } }

        private string _expiry = "";
        public string Expiry { get => _expiry; set { SetProperty(ref _expiry, value); OnPropertyChanged(nameof(IsFormValid)); } }

        private string _cvv = "";
        public string Cvv { get => _cvv; set { SetProperty(ref _cvv, value); OnPropertyChanged(nameof(IsFormValid)); } }

        private string _cardHolder = "";
        public string CardHolder { get => _cardHolder; set { SetProperty(ref _cardHolder, value); OnPropertyChanged(nameof(IsFormValid)); } }

        private string _email = "";
        public string Email { get => _email; set { SetProperty(ref _email, value); OnPropertyChanged(nameof(IsFormValid)); } }

        public bool IsFormValid => IsValid();
        private bool IsValid()
        {
            ClearErrors();
            if (CardNumber.Length != 16)
            {
                SetError("Номер карты должен содержать 16 цифр");
                return false;
            }
            if (Expiry.Length != 4)
            {
                SetError("Срок действия должен содержать 4 цифры");
                return false;
            }
            if (Cvv.Length != 3)
            {
                SetError("CVV/CVC должен содержать 3 цифры");
                return false;
            }
            if (string.IsNullOrWhiteSpace(CardHolder) && CardHolder.Length < 2)
            {
                SetError("Имя держателя карты должно быть больше 2 символов");
                return false;
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                SetError("Адрес электронной почты не должен быть пустым");
                return false;
            }
            if (!IsValidEmail(Email))
            {
                SetError("Неправльный формат адреса электронной почты");
                return false;
            }
            return true;
      }

        #endregion

        public PaymentViewModel(string token, int bookingId, string friendName,
            DateTime date, TimeSpan startTime, TimeSpan endTime, decimal totalAmount)
        {
            Token = token;
            BookingId = bookingId;
            FriendName = friendName;
            TotalAmount = totalAmount;

            DateTimeDisplay = $"{date:dd.MM.yyyy}, {startTime:hh\\:mm} – {endTime:hh\\:mm}";

            var duration = endTime - startTime;
            DurationDisplay = duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours} ч {duration.Minutes} мин"
                : $"{duration.Minutes} мин";

            Title = "Оплата бронирования";
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-z]+\.[a-z]{2,10}$");
        }
    }
}