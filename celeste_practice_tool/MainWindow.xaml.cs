using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace celeste_practice_tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CelesteProcess cp = new();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = cp.Context;
        }

        private void DataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            if (dg.SelectedIndex != -1)
            {
                dg.Dispatcher.Invoke(() =>
                {
                    dg.UpdateLayout();
                    dg.ScrollIntoView(dg.SelectedItem);
                });
            }
        }

        private void dumpCsv(object sender, RoutedEventArgs e)
        {
            string csv = cp.Context.dumpCsv();
            SaveFileDialog dialogue = new();

            dialogue.DefaultExt = "csv";
            dialogue.RestoreDirectory = true;
            dialogue.Filter = "CSV file (.csv)|*.csv";

            try
            {
                if (dialogue.ShowDialog() != true)
                {
                    return;
                }
                Stream s = dialogue.OpenFile();
                if (s == null)
                {
                    return;
                }
                using (StreamWriter writer = new(s))
                {
                    writer.Write(csv);
                }
                cp.Context.StatusText = "CSV exported successfully.";
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Failed to save file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("Failed to save file: file is being used by another program.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}