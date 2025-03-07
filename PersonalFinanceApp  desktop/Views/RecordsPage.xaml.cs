using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using static PersonalFinanceApp.MainWindow;
using System.Windows.Data;
using System.Linq;

namespace PersonalFinanceApp
{
    public partial class RecordsPage : Page
    {
        public ObservableCollection<Transaction> Transactions { get; set; }
        public ObservableCollection<Paystub> Paystubs { get; set; }

        public ICollectionView TransactionsView { get; set; }
        public ICollectionView PaystubsView { get; set; }

        
        private bool isEditingTransaction = false;
        private bool isEditingPaystub = false;

        public ICommand DeleteTransactionCommand { get; }
        public ICommand DeletePaystubCommand { get; }

        public RecordsPage()
        {
            InitializeComponent();
            DeleteTransactionCommand = new RelayCommand<Transaction>(transaction => DeleteTransaction(transaction));
            DeletePaystubCommand = new RelayCommand<Paystub>(paystub => DeletePaystub(paystub));
            Transactions = new ObservableCollection<Transaction>();
            Paystubs = new ObservableCollection<Paystub>();
            DataContext = this;
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                Transactions.Clear();
                Paystubs.Clear();

                // Load Transactions
                string transactionQuery = "SELECT Id, Amount, Category, Date, Description FROM Transactions;";
                var transactionsTable = DatabaseHelper.ExecuteQuery(transactionQuery);

                foreach (DataRow row in transactionsTable.Rows)
                {
                    if (!IsRowValid(row, new[] { "Amount", "Category", "Date", "Description" }))
                        continue;

                    Transactions.Add(new Transaction
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Amount = row["Amount"].ToString(),
                        Category = row["Category"].ToString(),
                        Date = row["Date"].ToString(),
                        Description = row["Description"].ToString()
                    });
                }

                // Load Paystubs
                string payStubQuery = "SELECT Id, Income, Date, Employer, Description FROM PayStubs;";
                var payStubsTable = DatabaseHelper.ExecuteQuery(payStubQuery);

                foreach (DataRow row in payStubsTable.Rows)
                {
                    if (!IsRowValid(row, new[] { "Income", "Date", "Employer", "Description" }, new[] { "Description" }))
                        continue;

                    var paystub = new Paystub
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Income = row["Income"].ToString(),
                        Date = row["Date"].ToString(),
                        Employer = row["Employer"].ToString(),
                        Description = row["Description"] == DBNull.Value ? "" : row["Description"].ToString() // ✅ Handle NULL values properly
                    };

                    Paystubs.Add(paystub);
                }


                // Apply Sorting to Transactions
                TransactionsView = CollectionViewSource.GetDefaultView(Transactions);
                TransactionsView.SortDescriptions.Clear();
                TransactionsView.SortDescriptions.Add(new SortDescription("ParsedDate", ListSortDirection.Descending));

                // Apply Sorting to Paystubs
                PaystubsView = CollectionViewSource.GetDefaultView(Paystubs);
                PaystubsView.SortDescriptions.Clear();
                PaystubsView.SortDescriptions.Add(new SortDescription("ParsedDate", ListSortDirection.Descending));

                TransactionsGrid.ItemsSource = TransactionsView;
                PaystubsGrid.ItemsSource = PaystubsView;
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error loading data: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }


        /// <summary>
        /// Checks if the specified DataRow has non-empty values for all specified columns.
        /// </summary>
        private bool IsRowValid(DataRow row, string[] columns, string[] optionalColumns = null)
        {
            optionalColumns ??= Array.Empty<string>(); // Ensure optionalColumns is not null

            foreach (var column in columns)
            {
                if (optionalColumns.Contains(column)) // Skip validation for optional columns
                    continue;

                if (row.IsNull(column) || string.IsNullOrWhiteSpace(row[column]?.ToString()))
                {
                    Console.WriteLine($"Skipping row due to missing or empty '{column}': {string.Join(", ", columns.Select(c => row[c]?.ToString() ?? "NULL"))}");
                    return false;
                }
            }
            return true;
        }



        // Sends a confirmation message
        private MessageBoxResult getConfirmation(string info)
        {
            return MessageBox.Show(info,
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        }

        private void DeleteTransaction(Transaction transaction)
        {
            if (transaction == null) return;

            // Show Confirmation Dialog
            if (getConfirmation($"Are you sure that you want to delete the transaction: {transaction.Description}?") == MessageBoxResult.No) return;

            try
            {
                // Delete from database
                string query = "DELETE FROM Transactions WHERE Id = @Id";
                var parameters = new Dictionary<string, object> { { "@Id", transaction.Id } };
                DatabaseHelper.ExecuteNonQuery(query, parameters);

                // Remove from ObservableCollection
                Transactions.Remove(transaction);

                MainWindow.Instance.ShowNotification("Transaction deleted successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error deleting transaction: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private void DeletePaystub(Paystub paystub)
        {
            if (paystub == null) return;

            // Show Confirmation Dialog
            if (getConfirmation($"Are you sure that you want to delete the paystub from {paystub.Employer}?") == MessageBoxResult.No) return;

            try
            {
                // Delete from database
                string query = "DELETE FROM Paystubs WHERE Id = @Id";
                var parameters = new Dictionary<string, object> { { "@Id", paystub.Id } };
                DatabaseHelper.ExecuteNonQuery(query, parameters);

                // Remove from ObservableCollection
                Paystubs.Remove(paystub);

                MainWindow.Instance.ShowNotification("Paystub deleted successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error deleting paystub: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private bool ValidateTransaction(Transaction transaction, out string errorMessage)
        {
            string[] validCategories = { "Rent", "Gas", "Food", "Entertainment", "Savings", "Monthly", "Maintenance", "Other" };
            if (!Array.Exists(validCategories, category => category == transaction.Category))
            {
                errorMessage = "Invalid category. Please select a valid category.";
                return false;
            }

            if (!DateTime.TryParseExact(transaction.Date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
            {
                errorMessage = "Invalid date format. Please use YYYY-MM-DD.";
                return false;
            }

            if (!double.TryParse(transaction.Amount, out double amount) || amount <= 0)
            {
                errorMessage = "Amount must be a positive number.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool ValidatePaystub(Paystub paystub, out string errorMessage)
        {
            if (!DateTime.TryParseExact(paystub.Date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
            {
                errorMessage = "Invalid date format. Please use YYYY-MM-DD.";
                return false;
            }

            if (!decimal.TryParse(paystub.Income, out decimal income) || income <= 0)
            {
                errorMessage = "Income must be a positive number.";
                return false;
            }

            errorMessage = null;
            return true;
        }



        private void TransactionsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isEditingTransaction) return;

            try
            {
                isEditingTransaction = true;

                // Get the edited row and column
                if (e.Row.Item is Transaction editedTransaction)
                {
                    // Keep a copy of the original transaction
                    var originalTransaction = new Transaction
                    {
                        Category = editedTransaction.Category,
                        Amount = editedTransaction.Amount,
                        Date = editedTransaction.Date,
                        Description = editedTransaction.Description
                    };

                    TransactionsGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    if (!ValidateTransaction(editedTransaction, out string errorMessage))
                    {
                        MainWindow.Instance.ShowNotification(errorMessage, NotificationType.Error);

                        // Revert changes to the original values
                        editedTransaction.Category = originalTransaction.Category;
                        editedTransaction.Amount = originalTransaction.Amount;
                        editedTransaction.Date = originalTransaction.Date;
                        editedTransaction.Description = originalTransaction.Description;

                        TransactionsGrid.Items.Refresh(); // Refresh the grid to show reverted values
                        return;
                    }

                    UpdateTransactionInDatabase(editedTransaction);
                }
            }
            finally
            {
                isEditingTransaction = false;
            }
        }

        private void PaystubsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isEditingPaystub) return;

            try
            {
                isEditingPaystub = true;

                // Get the edited row and column
                if (e.Row.Item is Paystub editedPaystub)
                {
                    // Keep a copy of the original paystub
                    var originalPaystub = new Paystub
                    {
                        Date = editedPaystub.Date,
                        Income = editedPaystub.Income,
                        Employer = editedPaystub.Employer,
                        Description = editedPaystub.Description
                    };

                    PaystubsGrid.CommitEdit(DataGridEditingUnit.Row, true);

                    if (!ValidatePaystub(editedPaystub, out string errorMessage))
                    {
                        MainWindow.Instance.ShowNotification(errorMessage, NotificationType.Error);

                        // Revert changes to the original values
                        editedPaystub.Date = originalPaystub.Date;
                        editedPaystub.Income = originalPaystub.Income;
                        editedPaystub.Employer = originalPaystub.Employer;
                        editedPaystub.Description = originalPaystub.Description;

                        PaystubsGrid.Items.Refresh(); // Refresh the grid to show reverted values
                        return;
                    }

                    UpdatePaystubInDatabase(editedPaystub);
                }
            }
            finally
            {
                isEditingPaystub = false;
            }
        }




        private void UpdateTransactionInDatabase(Transaction transaction)
        {
            try
            {
                // Convert Amount from string to double for database storage
                if (!double.TryParse(transaction.Amount, out double amount))
                {
                    MainWindow.Instance.ShowNotification("Failed to save transaction: Amount is not a valid number.", NotificationType.Error);
                    return;
                }

                string query = "UPDATE Transactions SET Amount = @Amount, Category = @Category, Date = @Date, Description = @Description WHERE Id = @Id";
                var parameters = new Dictionary<string, object>
        {
            { "@Amount", amount }, // Store as a number
            { "@Category", transaction.Category },
            { "@Date", transaction.Date },
            { "@Description", transaction.Description },
            { "@Id", transaction.Id }
        };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MainWindow.Instance.ShowNotification("Transaction updated successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error updating transaction: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }


        private void UpdatePaystubInDatabase(Paystub paystub)
        {
            try
            {
                // Convert Income from string to decimal for database storage
                if (!decimal.TryParse(paystub.Income, out decimal income))
                {
                    MainWindow.Instance.ShowNotification("Failed to save paystub: Income is not a valid number.", NotificationType.Error);
                    return;
                }

                string query = "UPDATE Paystubs SET Income = @Income, Date = @Date, Employer = @Employer, Description = @Description WHERE Id = @Id";
                var parameters = new Dictionary<string, object>
        {
            { "@Income", income }, // Store as a number
            { "@Date", paystub.Date },
            { "@Employer", paystub.Employer },
            { "@Description", paystub.Description },
            { "@Id", paystub.Id }
        };

                DatabaseHelper.ExecuteNonQuery(query, parameters);
                MainWindow.Instance.ShowNotification("Paystub updated successfully!", NotificationType.Success);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowNotification($"Error updating paystub: {ex.Message}", NotificationType.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        private void TransactionsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Validate the row data
            if (e.Row.Item is Transaction transaction)
            {
                if (string.IsNullOrWhiteSpace(transaction.Category) ||
                    string.IsNullOrWhiteSpace(transaction.Date) ||
                    string.IsNullOrWhiteSpace(transaction.Description))
                {
                    // Remove invalid rows from the source
                    Transactions.Remove(transaction);
                }
            }
        }

        private void PaystubsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is Paystub paystub)
            {
                if (string.IsNullOrWhiteSpace(paystub.Date) ||
                    string.IsNullOrWhiteSpace(paystub.Employer))
                {
                    Paystubs.Remove(paystub);
                    Console.WriteLine($"Removed paystub due to missing required fields: ID={paystub.Id}, Date={paystub.Date}, Employer={paystub.Employer}, Income={paystub.Income}");
                }
            }
        }



        public class Transaction
        {
            public int Id { get; set; }
            public string Category { get; set; }
            public string Amount { get; set; } // Stored as string
            public string Date { get; set; }
            public string Description { get; set; }

            // Computed property for sorting
            public DateTime ParsedDate
            {
                get
                {
                    if (DateTime.TryParse(Date, out DateTime parsedDate))
                        return parsedDate;
                    return DateTime.MinValue; // Fallback if parsing fails
                }
            }
        }

        public class Paystub
        {
            public int Id { get; set; }
            public string Date { get; set; }
            public string Income { get; set; } // Stored as string
            public string Employer { get; set; }
            public string Description { get; set; }

            // Computed property for sorting
            public DateTime ParsedDate
            {
                get
                {
                    if (DateTime.TryParse(Date, out DateTime parsedDate))
                        return parsedDate;
                    return DateTime.MinValue; // Fallback if parsing fails
                }
            }
        }



        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Predicate<T> _canExecute;

            public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute((T)parameter);
            }

            public void Execute(object parameter)
            {
                _execute((T)parameter);
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }


    }
}
