using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Weather
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ClickHandler(object sender, RoutedEventArgs args)
        {
            ((Weather)DataContext).Parse(myTextBox.Text);
            myTextBox.Text = null;
        }
    }
}