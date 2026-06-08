using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IsoSplitProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ScriptWindow : Window
    {
        public ScriptWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => Dispatcher.BeginInvoke(
            new Action(() => Mouse.OverrideCursor = Cursors.Arrow),
            DispatcherPriority.ApplicationIdle);

            Closed += (_, __) => Mouse.OverrideCursor = null;
        }
        private void Close_GUI(object sender, RoutedEventArgs e)
        {
            Close(); // this closes the script GUI
        }
    }
}
