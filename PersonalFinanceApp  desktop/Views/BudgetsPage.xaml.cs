using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PersonalFinanceApp.MainWindow;
using static PersonalFinanceApp.RecordsPage;

namespace PersonalFinanceApp.Views
{
    public partial class BudgetsPage : Page
    {
        public ObservableCollection<Budget> Budgets { get; set; }
        public ICommand DeleteBudgetCommand { get; }

        private bool isEditingBudget = false;

        private static readonly string[] PredefinedCategories =
        {
            "Rent", "Gas", "Food", "Entertainment", "Savings", "Monthly", "Maintenance", "Other"
        };

        public BudgetsPage()
        {
            InitializeComponent();
            DeleteBudgetCommand = new RelayCommand<Budget>(budget => DeleteBudget(budget));
            Budgets = new ObservableCollection<Budget>();
            DataContext = this;
            LoadBudgets();

            Budgets.CollectionChanged += (s, e) => UpdateTotalBudget(); // Update total when collection changes
            UpdateTotalBudget(); // Initial update
            BudgetsGrid.ItemsSource = Budgets;
            CategoryPicker.ItemsSource = PredefinedCategories;
        }

        private void LoadBudgets()
        {
            try
            {
                string query = "SELECT Id, Category, Amount FROM Budgets;";
                var budgetsTable = DatabaseHelper.ExecuteQuery(query);

                foreach (DataRow row in budgetsTable.Rows)
                {
                    Budgets.Add(new Budget
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Category = row["Category"].ToString(),
                        Amount = Convert.ToDouble(row["Amount"])
                    });
                }

                UpdateBudgetPieChart(); // Update chart after loading budgets
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error loading budgets: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private void UpdateTotalBudget()
        {
            double totalBudget = Budgets.Sum(b => b.Amount);
            TotalBudgetText.Text = $"${totalBudget:0.00}";
        }


        private void BudgetsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isEditingBudget) return;

            try
            {
                isEditingBudget = true;

                // Get the edited row
                if (e.Row.Item is Budget editedBudget)
                {
                    // Keep a copy of the original budget
                    var originalBudget = new Budget
                    {
                        Id = editedBudget.Id,
                        Category = editedBudget.Category,
                        Amount = editedBudget.Amount
                    };

                    // Commit the edit
                    BudgetsGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    // If editing the "Category" column
                    if (e.Column.Header.ToString() == "Category")
                    {
                        // Ensure uniqueness
                        if (Budgets.Any(b => b.Category == editedBudget.Category && b.Id != editedBudget.Id))
                        {
                            MainWindow.Instance.ShowNotification($"A budget for the category '{editedBudget.Category}' already exists.", NotificationType.Error);

                            // Revert changes to the original category
                            editedBudget.Category = originalBudget.Category;

                            BudgetsGrid.Items.Refresh(); // Refresh the grid to show reverted values
                            return;
                        }
                    }

                    // If editing the "Budget Amount" column
                    if (e.Column.Header.ToString() == "Budget Amount" && e.EditingElement is TextBox editingElement)
                    {
                        string editedValue = editingElement.Text;
                        if (!double.TryParse(editedValue, out double newAmount) || newAmount < 0)
                        {
                            MainWindow.Instance.ShowNotification("Invalid budget amount. Please enter a valid positive number.", NotificationType.Error);

                            // Revert changes to original values
                            editedBudget.Amount = originalBudget.Amount;

                            BudgetsGrid.CancelEdit(DataGridEditingUnit.Row); // Cancel the edit
                            BudgetsGrid.Items.Refresh(); // Refresh the grid
                            return;
                        }

                        // Update the amount in the editedBudget object
                        editedBudget.Amount = newAmount;
                    }

                    // Validate the entire budget object
                    if (!ValidateBudget(editedBudget, out string errorMessage))
                    {
                        MainWindow.Instance.ShowNotification(errorMessage, NotificationType.Error);

                        // Revert changes to original values
                        editedBudget.Category = originalBudget.Category;
                        editedBudget.Amount = originalBudget.Amount;

                        BudgetsGrid.Items.Refresh(); // Refresh the grid
                        return;
                    }

                    // Update the budget in the database
                    UpdateBudgetInDatabase(editedBudget);
                    UpdateBudgetPieChart(); // Update chart after editing a budget
                    UpdateTotalBudget();
                }
            }
            finally
            {
                isEditingBudget = false;
            }
        }






        private void AddBudget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var category = CategoryPicker.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(category))
                {
                    MainWindow.Instance.ShowNotification("Please select a valid category.", NotificationType.Warning);
                    return;
                }

                if (Budgets.Any(b => b.Category == category))
                {
                    MainWindow.Instance.ShowNotification($"A budget for the category '{category}' already exists.", NotificationType.Warning);
                    return;
                }

                if (!double.TryParse(BudgetAmountInput.Text, out double amount) || amount <= 0)
                {
                    MainWindow.Instance.ShowNotification("Please enter a valid positive amount.", NotificationType.Warning);
                    return;
                }

                var newBudget = new Budget
                {
                    Id = -1, // Temporary ID until inserted into the database
                    Category = category,
                    Amount = amount
                };

                AddBudgetToDatabase(newBudget);
                Budgets.Add(newBudget);

                UpdateBudgetPieChart(); // Update chart after adding a budget
                UpdateTotalBudget();

                MainWindow.Instance.ShowNotification("Budget added successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error adding budget: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }


        private void AddBudgetToDatabase(Budget budget)
        {
            try
            {
                string query = "INSERT INTO Budgets (Category, Amount) VALUES (@Category, @Amount)";
                var parameters = new Dictionary<string, object>
                {
                    { "@Category", budget.Category },
                    { "@Amount", budget.Amount }
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error inserting budget into database: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private void UpdateBudgetInDatabase(Budget budget)
        {
            try
            {
                string query = "UPDATE Budgets SET Category = @Category, Amount = @Amount WHERE Id = @Id;";
                var parameters = new Dictionary<string, object>
                {
                    { "@Category", budget.Category },
                    { "@Amount", budget.Amount },
                    { "@Id", budget.Id }
                };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MainWindow.Instance.ShowNotification("Budget updated successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error updating budget: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private void DeleteBudget(Budget budget)
        {
            if (budget == null) return;

            if (MessageBox.Show($"Are you sure you want to delete the budget for category: {budget.Category}?",
                                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                string query = "DELETE FROM Budgets WHERE Id = @Id;";
                var parameters = new Dictionary<string, object> { { "@Id", budget.Id } };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                Budgets.Remove(budget);
                UpdateBudgetPieChart(); // Update chart after deleting a budget
                UpdateTotalBudget();

                MainWindow.Instance.ShowNotification("Budget deleted successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error deleting budget: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private bool ValidateBudget(Budget budget, out string errorMessage)
        {
            if (!Array.Exists(PredefinedCategories, category => category == budget.Category))
            {
                errorMessage = "Invalid category. Please select a valid category.";
                return false;
            }

            if (Budgets.Any(b => b.Category == budget.Category && b.Id != budget.Id))
            {
                errorMessage = $"A budget for the category '{budget.Category}' already exists.";
                return false;
            }

            if (budget.Amount <= 0)
            {
                errorMessage = "Budget amount must be a positive number.";
                return false;
            }

            errorMessage = null;
            return true;
        }


        private void BudgetsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is Budget budget)
            {
                if (string.IsNullOrWhiteSpace(budget.Category) || budget.Amount <= 0)
                {
                    Budgets.Remove(budget);
                }
            }
        }

        private void UpdateBudgetPieChart()
        {
            try
            {
                // Clear previous data
                BudgetPieChart.Series.Clear();

                // Check if there is data
                if (Budgets == null || !Budgets.Any())
                {
                    BudgetPieChart.Visibility = Visibility.Collapsed;
                    BudgetPieChartNoDataMessage.Visibility = Visibility.Visible;
                    return;
                }

                BudgetPieChart.Visibility = Visibility.Visible;
                BudgetPieChartNoDataMessage.Visibility = Visibility.Collapsed;

                // Create the pie chart series
                var seriesCollection = new LiveCharts.SeriesCollection();
                foreach (var budget in Budgets)
                {
                    seriesCollection.Add(new LiveCharts.Wpf.PieSeries
                    {
                        Title = budget.Category,
                        Values = new LiveCharts.ChartValues<double> { budget.Amount },
                        DataLabels = true, // Show labels on the pie chart
                        LabelPoint = chartPoint => $"${chartPoint.Y:0.00}"
                    });
                }

                BudgetPieChart.Series = seriesCollection;
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error updating budget pie chart: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error in UpdateBudgetPieChart: {ex}");
            }
        }


        public class Budget
        {
            public int Id { get; set; }
            public string Category { get; set; }
            public double Amount { get; set; }
        }
    }
}
