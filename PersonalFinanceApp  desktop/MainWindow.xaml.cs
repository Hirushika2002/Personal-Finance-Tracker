using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PersonalFinanceApp
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance => Application.Current.MainWindow as MainWindow;
        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();

            // Load the Dashboard by default
            DynamicContentFrame.Navigate(new DashboardPage());

            // Dynamically set the window size to 80% of the screen size
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Set window dimensions as a percentage of screen size
            this.Width = screenWidth * 0.8;
            this.Height = screenHeight * 0.8;

            // Optionally, center the window on the screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void NavigateToDashboard(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new DashboardPage());
        }

        private void NavigateToTransactions(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new TransactionsPage());
        }

        private void NavigateToReports(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new ReportsPage());
        }

        private void NavigateToPayStub(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new PayStubPage());
        }

        private void NavigateToRecords(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new RecordsPage());
        }

        private void NavigateToBudgets(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new Views.BudgetsPage());
        }

        private void NavigateToAdmin(object sender, RoutedEventArgs e)
        {
            DynamicContentFrame.Navigate(new Views.AdminPage());
        }

        public enum NotificationType
        {
            Error,
            Success,
            Info,
            Warning,
            Critical
        }

        public void ShowNotification(string message, NotificationType type)
        {
            // Create a new NotificationControl
            var notification = new NotificationControl
            {
                Message = message
            };

            // Set the background color based on the type
            SolidColorBrush backgroundColor;
            switch (type)
            {
                case NotificationType.Error:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(168, 50, 50)); // Dark Red
                    break;
                case NotificationType.Success:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(50, 168, 82)); // Dark Green
                    break;
                case NotificationType.Info:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(50, 92, 168)); // Dark Blue
                    break;
                case NotificationType.Warning:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(204, 153, 0)); // Dark Yellow
                    break;
                case NotificationType.Critical:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(139, 0, 0)); // Dark Crimson
                    break;
                default:
                    backgroundColor = new SolidColorBrush(Color.FromRgb(50, 50, 50)); // Default Dark Grey
                    break;
            }
            notification.BackgroundColor = backgroundColor;

            // Add the notification to the panel
            NotificationPanel.Children.Add(notification);

            // Auto-remove notification after 5 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) =>
            {
                NotificationPanel.Children.Remove(notification);
                timer.Stop();
            };
            timer.Start();
        }


    }
}
