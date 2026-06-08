using System;
using System.Windows;
using System.Windows.Input;

namespace IsoSplitProject.Helpers
{
    public class MoveFocusCommand : ICommand
    {
        public bool CanExecute(object parameter) => true;
        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            var element = Keyboard.FocusedElement as UIElement;
            element?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            //Keyboard.ClearFocus();
        }
    }
}
