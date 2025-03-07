using System;
using System.Windows;
using System.Windows.Controls;
using static PersonalFinanceApp.MainWindow;

namespace PersonalFinanceApp
{
    /// <summary>
    /// Interaction logic for PayStubPage.xaml
    /// </summary>
    public partial class PayStubPage : Page
    {
        public PayStubPage()
        {
            InitializeComponent();
        }

        private void SavePayStub(object sender, RoutedEventArgs e)
        {
            // Fetch input values
            string incomeAmount = IncomeAmountInput.Text;
            DateTime? selectedDate = IncomeDatePicker.SelectedDate;
            string employer = EmployerInput.Text;
            string description = IncomeDescriptionInput.Text;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(incomeAmount) || !decimal.TryParse(incomeAmount, out decimal income))
            {
                MainWindow.Instance.ShowNotification("Please enter a valid income amount.", NotificationType.Error);
                return;
            }

            if (selectedDate == null)
            {
                MainWindow.Instance.ShowNotification("Please select a date.", NotificationType.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(employer))
            {
                MainWindow.Instance.ShowNotification("Please enter the employer/source.", NotificationType.Error);
                return;
            }

            // Save to database
            try
            {
                string query = @"
            INSERT INTO PayStubs (Income, Date, Employer, Description)
            VALUES (@Income, @Date, @Employer, @Description);";

                var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "@Income", income },
            { "@Date", selectedDate.Value.ToString("yyyy-MM-dd") },
            { "@Employer", employer },
            { "@Description", description }
        };

                DatabaseHelper.ExecuteNonQuery(query, parameters);

                // Clear form and show success notification
                ClearForm();
                MainWindow.Instance.ShowNotification("Pay stub saved successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                // Show error notification
                MainWindow.Instance.ShowNotification($"Error saving pay stub: {ex.Message}", NotificationType.Error);
            }
        }

        private void ClearForm()
        {
            IncomeAmountInput.Text = string.Empty;
            IncomeDatePicker.SelectedDate = null;
            EmployerInput.Text = string.Empty;
            IncomeDescriptionInput.Text = string.Empty;
        }

        private void CancelPayStub(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }
    }
}
