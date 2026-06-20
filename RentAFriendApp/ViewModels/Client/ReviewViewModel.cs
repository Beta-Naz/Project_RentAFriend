using System.Windows;
using System.Windows.Input;
using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.ReviewDTO;
using RentAFriendApp.ViewModels.Base;

namespace RentAFriendApp.ViewModels.Client
{
    internal class ReviewViewModel : BaseViewModel
    {
        private readonly string _token;
        private readonly int _bookingId;

        #region Свойства

        private string _friendName = "";
        public string FriendName
        {
            get => _friendName;
            set => SetProperty(ref _friendName, value);
        }

        private string _friendInitials = "?";
        public string FriendInitials
        {
            get => _friendInitials;
            set => SetProperty(ref _friendInitials, value);
        }

        private DateTime _bookingDate;
        public DateTime BookingDate
        {
            get => _bookingDate;
            set => SetProperty(ref _bookingDate, value);
        }

        private string _bookingTime = "";
        public string BookingTime
        {
            get => _bookingTime;
            set => SetProperty(ref _bookingTime, value);
        }

        private string _purpose = "";
        public string Purpose
        {
            get => _purpose;
            set => SetProperty(ref _purpose, value);
        }

        private int _rating;
        public int Rating
        {
            get => _rating;
            set
            {
                if(SetProperty(ref _rating, value))
                {
                    ClearErrors();
                }
            }
        }

        private string _titleReview = "";
        public string TitleReview
        {
            get => _titleReview;
            set
            {
                if (SetProperty(ref _titleReview, value))
                {
                    ClearErrors();
                }
            }
        }

        private string _reviewText = "";
        public string ReviewText
        {
            get => _reviewText;
            set
            {
                if (SetProperty(ref _reviewText, value))
                {
                    ClearErrors();
                }
            }
        }

        public bool IsValid => IsValidForm();
        #endregion

        #region Команды
        public ICommand SubmitReviewCommand { get; }
        #endregion


        public ReviewViewModel(string token, int bookingId)
        {
            _token = token;
            _bookingId = bookingId;

            SubmitReviewCommand = new RelayCommandAsync(SubmitReviewAsync);
            _ = LoadBookingInfoAsync();
        }

        private async Task LoadBookingInfoAsync()
        {
            try
            {
                IsBusy = true;

                var details = await BookingContext.GetBookingDetails(_token, _bookingId);
                if (details?.Booking == null)
                {
                    SetError("Не удалось загрузить информацию о бронировании");
                    return;
                }

                var b = details.Booking;
                FriendName = b.FriendName;
                FriendInitials = string.Concat(
                    (b.FriendName ?? "?").Split(' ').Take(2).Select(w => w[0])
                ).ToUpper();
                BookingDate = b.ScheduleDate;
                BookingTime = $"{b.StartTime:hh\\:mm} – {b.EndTime:hh\\:mm}";
                Purpose = b.Purpose;
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SubmitReviewAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                var reviewData = new CreateReviewDTO
                {
                    BookingID = _bookingId,
                    Rating = Rating,
                    Title = Title.Trim(),
                    Comment = ReviewText.Trim()
                };

                var result = await ReviewContext.CreateReview(_token, reviewData);

                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "🎉 Спасибо за отзыв!",
                            "Успешно",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        if (MainWindow.Instanse?.MainFrame.CanGoBack == true)
                            MainWindow.Instanse.MainFrame.GoBack();
                    });
                }
                else
                {
                    SetError("Не удалось отправить отзыв");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        public bool IsValidForm()
        {
            if (Rating < 1 || Rating > 5)
            {
                SetError("Пожалуйста, поставьте оценку");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TitleReview))
            {
                SetError("Заголовок не должен быть пустым");
                return false;
            }

            if (TitleReview.Trim().Length > 80)
            {
                SetError("Заголовок не должен быть больше 80 символов");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TitleReview) || ReviewText.Trim().Length < 20)
            {
                SetError("Отзыв должен содержать минимум 20 символов");
                return false;
            }
            return true;
        }
    }
}