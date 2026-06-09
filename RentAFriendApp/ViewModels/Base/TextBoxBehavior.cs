using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RentAFriendApp.ViewModels.Base
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty LostFocusCommandProperty =
            DependencyProperty.RegisterAttached(
                "LostFocusCommand",
                typeof(ICommand),
                typeof(TextBoxBehavior),
                new PropertyMetadata(null, OnLostFocusCommandChanged));

        public static ICommand GetLostFocusCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(LostFocusCommandProperty);
        }

        public static void SetLostFocusCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(LostFocusCommandProperty, value);
        }

        private static void OnLostFocusCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if (e.OldValue != null)
                {
                    textBox.LostFocus -= OnLostFocus;
                }
                if (e.NewValue != null)
                {
                    textBox.LostFocus += OnLostFocus;
                }
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                var command = GetLostFocusCommand(textBox);
                if (command != null && command.CanExecute(textBox.Text))
                {
                    command.Execute(textBox.Text);
                }
            }
        }
    }
}
