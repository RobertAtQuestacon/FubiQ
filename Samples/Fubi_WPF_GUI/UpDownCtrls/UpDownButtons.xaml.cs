using System.Windows;

namespace Fubi_WPF_GUI.UpDownCtrls
{
    /// <summary>
    /// Interaction logic for UpDownButtons.xaml
    /// </summary>
    public partial class UpDownButtons
    {
        public static readonly RoutedEvent UpClickEvent = EventManager.RegisterRoutedEvent("UpClick",
                                     RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UpDownButtons));

        public event RoutedEventHandler UpClick
        {
            add { AddHandler(UpClickEvent, value); }
            remove { RemoveHandler(UpClickEvent, value); }
        }

        public static readonly RoutedEvent DownClickEvent = EventManager.RegisterRoutedEvent("DownClick",
                             RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UpDownButtons));

        public event RoutedEventHandler DownClick
        {
            add { AddHandler(DownClickEvent, value); }
            remove { RemoveHandler(DownClickEvent, value); }
        }
        public UpDownButtons()
        {
            InitializeComponent();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var upClickEventArgs = new RoutedEventArgs(UpClickEvent);
            RaiseEvent(upClickEventArgs);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            var downClickEventArgs = new RoutedEventArgs(DownClickEvent);
            RaiseEvent(downClickEventArgs);
        }
    }
}