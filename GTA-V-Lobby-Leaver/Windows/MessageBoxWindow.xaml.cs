using System.Windows;
using System.Windows.Controls.Primitives;

namespace GTA_V_Lobby_Leaver
{
    public partial class MessageBoxWindow : Window
    {
        public MessageBoxWindow(Window windowOwner, string textMessage, string caption, string button, Config config)
        {
            InitializeComponent();

            Owner = windowOwner;
            Title = caption;
            txbTextMessage.Text = textMessage;

            if (button == "YesNo")
            {
                if (config == null || config.Language == 0) { btnResponseYes.Content = "Yes!"; btnResponseNo.Content = "No"; }
                else if (config == null || config.Language == 1) { btnResponseYes.Content = "Ja!"; btnResponseNo.Content = "Nein"; }
            }

            if (button == "OKCancel")
            {
                btnResponseYes.Content = "Okay";
                if (config == null || config.Language == 0) {  btnResponseNo.Content = "Cancel"; }
                else if (config == null || config.Language == 1) { btnResponseNo.Content = "Abbrechen"; }
            }

            if (button == "OK")
            {
                btnResponseYes.Content = "Okay";
                btnResponseYes.HorizontalAlignment = HorizontalAlignment.Center;
                btnResponseYes.Margin = new Thickness(0, 1, 0, 0);
                btnResponseNo.Visibility = Visibility.Hidden;
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnResponseYes.Focus();
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void ResponseYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void ResponseNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            btnResponseYes.Focus();
        }
        private void Escape_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) { btnResponseYes.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); }
        }
    }
}