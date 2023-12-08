using System;
using System.Windows;
using System.Windows.Controls;

namespace EscapePod
{
    public class TextBoxWithPlaceholder : TextBox
    {
        public TextBoxWithPlaceholder()
        {
            GotFocus += TextBoxWithPlaceholder_GotFocus;
            LostFocus += TextBoxWithPlaceholder_LostFocus;
            Loaded += TextBoxWithPlaceholder_Loaded;
        }

        private void TextBoxWithPlaceholder_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Text = Placeholder;
            }
        }

        private void TextBoxWithPlaceholder_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Text == Placeholder)
            {
                Text = string.Empty;
            }
        }

        private void TextBoxWithPlaceholder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Text = Placeholder;
            }
        }

        public string Placeholder
        {
            get
            {
                return (string)GetValue(PlaceholderProperty);
            }

            set
            {
                SetValue(PlaceholderProperty, value);
            }
        }

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register("Placeholder", typeof(String), typeof(TextBoxWithPlaceholder));
    }
}
