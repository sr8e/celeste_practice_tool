using System.Windows;

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
    }
}