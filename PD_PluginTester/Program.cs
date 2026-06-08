using IsoSplitProject.Helpers;
using PluginTester;
using System;
using System.Windows.Forms;

namespace PD_PluginTester
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try 
            { 
                Application.Run(new PluginTesterForm()); 
            } 
            catch(
            Exception exception
            ) 
            { 
                var test = exception.Message; Logger.LogFatal(string.Format("Fatal error: {0}", test)); 
            }
        }
    }
}
