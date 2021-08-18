using System.Windows;

namespace GTA_V_Lobby_Leaver
{
    public static class MessageBox
    {
        public static bool ShowDialog(Window windowOwner, string textMessage, string caption, string button, Config config)
        {
            MessageBoxWindow messageBoxWindow = new MessageBoxWindow(windowOwner, textMessage, caption, button, config);
            messageBoxWindow.ShowDialog();

            if (messageBoxWindow.DialogResult == false) { return false; }
            else if (messageBoxWindow.DialogResult == true) { return true; }
            return false;
        }
    }
}