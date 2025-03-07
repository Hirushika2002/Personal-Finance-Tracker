using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PersonalFinanceApp
{
    public partial class NotificationControl : UserControl
    {
        public NotificationControl()
        {
            InitializeComponent();
            // Set DataContext to itself to bind directly to its properties
            DataContext = this;
        }

        // Message Dependency Property
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(NotificationControl), new PropertyMetadata(""));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        // BackgroundColor Dependency Property
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(NotificationControl), new PropertyMetadata(Brushes.Black));

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }
    }
}
