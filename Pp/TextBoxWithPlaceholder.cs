using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pp
{
    public class TextBoxWithPlaceholder : TextBox
    {
        private string placeholder;

        public TextBoxWithPlaceholder()
        {
            GotFocus += TextBoxWithPlaceholder_GotFocus;
            LostFocus += TextBoxWithPlaceholder_LostFocus;
            Loaded += TextBoxWithPlaceholder_Loaded;
        }

        private void TextBoxWithPlaceholder_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Text = placeholder;
            }
        }

        private void TextBoxWithPlaceholder_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Text == Placeholder)
            {
                Text = string.Empty;
            }
        }

        private void TextBoxWithPlaceholder_LostFocus(object sender, System.Windows.RoutedEventArgs e)
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
                return placeholder;
            }

            set
            {
                placeholder = value;
            }
        }
    }
}
