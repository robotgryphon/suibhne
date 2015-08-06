using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui {
    class TextEntryPrompt : Window {

        private TextBox entry;
        public Button confirm;

        public string Text {
            get {
                return entry.Text;
            }

            protected set { }
        }

        public TextEntryPrompt(String title, String text, String buttonText = "Okay") {
            this.Title = title;
            this.Width = 400;
            this.Height = 140;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;

            StackPanel container = new StackPanel();
            this.Content = container;
            container.Margin = new Thickness(10);

            TextBlock help = new TextBlock();
            help.Text = text;
            container.Children.Add(help);

            entry = new TextBox();
            container.Children.Add(entry);

            confirm = new Button();
            container.Children.Add(confirm);
            confirm.Content = buttonText;
            confirm.Margin = new Thickness(0, 6, 0, 0);
            confirm.Style = (Style)Application.Current.FindResource("FlatButton");
        }
    }
}
