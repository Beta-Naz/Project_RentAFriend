using RentAFriendApp.Context;
using RentAFriendApp.Models.ClassesDTO.BookingDTO;
using RentAFriendApp.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Friend
{
    internal class FriendBookingsViewModel : BaseViewModel
    {
        private readonly string _token;
        private int _profileId;

        // Коллекции
        private ObservableCollection<BookingDetailsDTO>? _items;
        public ObservableCollection<BookingDetailsDTO>? Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        // Фильтры
        private string _selectedStatus = "Pending";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    FilterItems();
                }
            }
        }

        // Доступные статусы для фильтрации
        private ObservableCollection<string>? _availableStatuses;
        public ObservableCollection<string>? AvailableStatuses
        {
            get => _availableStatuses;
            set => SetProperty(ref _availableStatuses, value);
        }

        // Команды
        public ICommand ViewBookingDetailsCommand { get; }
        public ICommand AcceptBookingCommand { get; }
        public ICommand RejectBookingCommand { get; }
        public ICommand CompleteBookingCommand { get; }
        public ICommand FilterByStatusCommand { get; }
        public ICommand MessageClientCommand { get; }
        public ICommand RefreshCommand { get; }

        // Вычисляемые свойства
        public int PendingCount => Items?.Count(b => b.Status == "Pending") ?? 0;
        public int ConfirmedCount => Items?.Count(b => b.Status == "Confirmed") ?? 0;
        public decimal TotalEarnings => Items?.Where(b => b.PaymentStatus == "Paid" && b.Status == "Completed").Sum(b => b.TotalAmount) ?? 0;
        public bool HasItems => Items?.Count > 0;

        public FriendBookingsViewModel(string token)
        {
            _token = token;
            Title = "Запросы на бронирование";

            Items = [];
            AvailableStatuses =
            [
                "Все",
                "Pending",
                "Confirmed",
                "Completed",
                "Cancelled",
                "Rejected"
            ];

            ViewBookingDetailsCommand = new RelayCommandAsync<BookingDetailsDTO>(ViewBookingDetails);
            AcceptBookingCommand = new RelayCommandAsync<BookingDetailsDTO>(AcceptBookingAsync, CanAcceptBooking);
            RejectBookingCommand = new RelayCommandAsync<BookingDetailsDTO>(RejectBookingAsync, CanRejectBooking);
            CompleteBookingCommand = new RelayCommandAsync<BookingDetailsDTO>(CompleteBookingAsync, CanCompleteBooking);
            FilterByStatusCommand = new RelayCommandAsync<string>(FilterByStatus);
            MessageClientCommand = new RelayCommandAsync<BookingDetailsDTO>(MessageClient);
            RefreshCommand = new RelayCommandAsync(LoadItemsAsync);

            _ = InitializeAsync();
        }
        private async Task InitializeAsync()
        {
            await LoadProfileAsync();
            await LoadItemsAsync();
        }
        private async Task LoadProfileAsync()
        {
            try
            {
                var user = await UserContext.GetUser(_token);
                var profilesResponse = await FriendProfileContext.GetAllProfiles(_token);
                var profile = profilesResponse?.Profiles?.FirstOrDefault(p => p.UserID == user?.Data?.UserID);

                if (profile != null)
                {
                    _profileId = profile.ProfileID;
                }
                else
                {
                    SetError("Профиль друга не найден");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        public async Task LoadItemsAsync()
        {
            try
            {
                IsBusy = true;
                ClearErrors();

                if (_profileId <= 0)
                {
                    await LoadProfileAsync();
                }

                if (_profileId <= 0)
                {
                    SetError("Профиль не загружен");
                    return;
                }

                string? statusFilter = SelectedStatus == "Все" ? null : SelectedStatus;
                var bookings = await BookingContext.GetFriendBookings(_token, _profileId, statusFilter);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Items?.Clear();

                    if (bookings?.Bookings != null)
                    {
                        foreach (var booking in bookings.Bookings)
                        {
                            Items?.Add(booking);
                        }
                    }
                });

                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(ConfirmedCount));
                OnPropertyChanged(nameof(TotalEarnings));
                OnPropertyChanged(nameof(HasItems));
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки запросов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanAcceptBooking(BookingDetailsDTO? booking)
        {
            return booking != null &&
                   booking.Status == "Pending" &&
                   !IsBusy;
        }

        private bool CanRejectBooking(BookingDetailsDTO? booking)
        {
            return booking != null &&
                   booking.Status == "Pending" &&
                   !IsBusy;
        }

        private bool CanCompleteBooking(BookingDetailsDTO? booking)
        {
            return booking != null &&
                   booking.Status == "Confirmed" &&
                   !IsBusy;
        }

        private async Task AcceptBookingAsync(BookingDetailsDTO? booking)
        {
            if (booking == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var result = await BookingContext.UpdateBookingStatus(_token, booking.BookingID, "Confirmed");

                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Бронирование #{booking.BookingID} подтверждено");
                    await LoadItemsAsync();
                }
                else
                {
                    SetError("Ошибка подтверждения бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка подтверждения бронирования: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RejectBookingAsync(BookingDetailsDTO? booking)
        {
            if (booking == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var result = await BookingContext.RejectBooking(_token, booking.BookingID);

                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Бронирование #{booking.BookingID} отклонено");
                    await LoadItemsAsync();
                }
                else
                {
                    SetError("Ошибка отклонения бронирования");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка отклонения бронирования: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CompleteBookingAsync(BookingDetailsDTO? booking)
        {
            if (booking == null) return;

            try
            {
                IsBusy = true;
                ClearErrors();

                var result = await BookingContext.UpdateBookingStatus(_token, booking.BookingID, "Completed");

                if (result != null)
                {
                    Base.Messenger.Default.SendNotification($"Встреча #{booking.BookingID} завершена");
                    await LoadItemsAsync();
                }
                else
                {
                    SetError("Ошибка завершения встречи");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка завершения встречи: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task MessageClient(BookingDetailsDTO? booking)
        {
            if (booking == null) return;

            try
            {
                // Открываем чат с клиентом
                var chat = await ChatContext.GetOrCreateChat(_token, booking.ClientId);
                if (chat != null)
                {
                    Messenger.Default.SendData(new
                    {
                        chat.ChatId,
                        FriendName = booking.ClientName
                    });
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка открытия чата: {ex.Message}");
            }
        }

        private async Task ViewBookingDetails(BookingDetailsDTO? booking)
        {
            if (booking == null)
            {
                MessageBox.Show("Нет данных для отображения.", "Детали бронирования",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Получаем детали бронирования
            var details = await BookingContext.GetBookingDetails(_token, booking.BookingID);

            if (details?.Booking != null)
            {
                string message = $@"
                ID бронирования: #{details.Booking.BookingID}
                Статус: {GetStatusDisplay(details.Booking.Status)}
                Сумма: {details.Booking.TotalAmount:C}
                Оплата: {GetPaymentStatusDisplay(details.Booking.PaymentStatus)}
                
                Клиент:
                  Имя: {details.Booking.ClientName}
                  Email: {details.Booking.ClientEmail}
                  Телефон: {details.Booking.ClientPhone}
                
                Встреча:
                  Дата: {details.Booking.ScheduleDate:dd.MM.yyyy}
                  Время: {details.Booking.StartTime:hh\\:mm} - {details.Booking.EndTime:hh\\:mm}
                  Место: {(string.IsNullOrEmpty(details.Booking.MeetingLocation) ? "Не указано" : details.Booking.MeetingLocation)}
                
                Цель: {(string.IsNullOrEmpty(details.Booking.Purpose) ? "Не указана" : details.Booking.Purpose)}
                
                Специальные пожелания:
                {(string.IsNullOrEmpty(details.Booking.SpecialRequests) ? "Нет" : details.Booking.SpecialRequests)}
                
                Создано: {details.Booking.CreatedAt:dd.MM.yyyy HH:mm}
                Обновлено: {details.Booking.UpdatedAt:dd.MM.yyyy HH:mm}
                ";

                MessageBox.Show(message.Trim(), "Детали бронирования",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task FilterByStatus(string? status)
        {
            if (status != null)
            {
                SelectedStatus = status;
                await LoadItemsAsync();
            }
        }

        private void FilterItems()
        {
            _ = LoadItemsAsync();
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает подтверждения",
                "Confirmed" => "Подтверждено",
                "Completed" => "Завершено",
                "Cancelled" => "Отменено",
                "Rejected" => "Отклонено",
                _ => status
            };
        }

        private string GetPaymentStatusDisplay(string paymentStatus)
        {
            return paymentStatus switch
            {
                "Paid" => "Оплачено",
                "Unpaid" => "Не оплачено",
                "Refunded" => "Возвращено",
                _ => paymentStatus
            };
        }
    }
}