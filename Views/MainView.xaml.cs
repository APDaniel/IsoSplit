using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IsoSplitProject.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }
        

        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void FieldIdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox)
                {
                    // Push TextBox.Text into the bound fieldID property
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

                    // Prevent Enter from being processed further
                    e.Handled = true;

                    // Move focus away from the TextBox
                    textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }

    }
}

        
    
