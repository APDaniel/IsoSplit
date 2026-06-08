using System;
using System.Windows.Input;

namespace IsoSplitProject.Commands
{
    
    /// <summary>
    /// This class represents a command relay to be used to initialize a method though a command layer
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Private Members
        /// <summary>
        /// Action to run
        /// </summary>
        private Action mAction;
        private Func<bool> mCanExecute;
        #endregion

        #region Public Events
        /// <summary>
        /// The event that is fired when the CanExecute value has changed
        /// </summary>
        public event EventHandler CanExecuteChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public RelayCommand(Action action, Func<bool> canExecute = null)
        {
            mAction = action;
            mCanExecute = canExecute;
        }
        #endregion

        #region ICommand Implementation
        /// <summary>
        /// Determines whether the command can execute or not
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return mCanExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// Executes the command action
        /// </summary>
        public void Execute(object parameter)
        {
            mAction?.Invoke();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
