using RentAFriendApp.ViewModels.Friend;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RentAFriendApp.Views.Friend.Controls
{
    public partial class TimeSlotItem : UserControl
    {
        public TimeSlotItem()
        {
            InitializeComponent();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var slot = DataContext as ScheduleViewModel.ScheduleSlot;
            if (slot == null) return;

            var scheduleViewModel = FindParent<SchedulePage>(this)?.DataContext as ScheduleViewModel;
            if (scheduleViewModel != null)
            {
                _ = scheduleViewModel.ToggleAvailabilityAsync(slot);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var slot = DataContext as ScheduleViewModel.ScheduleSlot;
            if (slot == null) return;

            var scheduleViewModel = FindParent<SchedulePage>(this)?.DataContext as ScheduleViewModel;
            if (scheduleViewModel != null)
            {
                _ = scheduleViewModel.RemoveTimeSlotAsync(slot);
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }
    }
}