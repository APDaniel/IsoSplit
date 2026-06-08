using System.Windows;
using System.Windows.Controls;

namespace IsoSplitProject.Helpers
{
    /// <summary>
    /// This class is required to bind ComboBox event to ICommand defined in the ViewModel
    /// </summary>
    public static class SelectionChangedBehavior
    {
        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.RegisterAttached(
                "SelectionChangedCommand",
                typeof(System.Windows.Input.ICommand),
                typeof(SelectionChangedBehavior),
                new PropertyMetadata(null, OnSelectionChangedCommandChanged));

        public static System.Windows.Input.ICommand GetSelectionChangedCommand(DependencyObject obj)
        {
            return (System.Windows.Input.ICommand)obj.GetValue(SelectionChangedCommandProperty);
        }

        public static void SetSelectionChangedCommand(DependencyObject obj, System.Windows.Input.ICommand value)
        {
            obj.SetValue(SelectionChangedCommandProperty, value);
        }

        private static void OnSelectionChangedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                if (e.NewValue is System.Windows.Input.ICommand command)
                {
                    comboBox.SelectionChanged += ComboBox_SelectionChanged;
                }
                else if (e.OldValue is System.Windows.Input.ICommand oldCommand)
                {
                    comboBox.SelectionChanged -= ComboBox_SelectionChanged;
                }
            }
        }

        private static void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var command = GetSelectionChangedCommand(comboBox);
                if (command != null && command.CanExecute(comboBox.SelectedItem))
                {
                    command.Execute(comboBox.SelectedItem);
                }
            }
        }
    }
}

