using System;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using System.Threading;
using IsoSplitProject.Helpers;
using IsoSplitProject.ViewModels;
using IsoSplitProject;
using System.Windows.Threading;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyFileVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.1")]
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    
    public class Script // This is the script that will be called by Eclipse. It is referenced by PD_PluginTester
    {
        IsoSplitViewModel mainViewModel = null;

        public static ScriptContext scriptContext;
        public static ScriptWindow mainWindow;
        public Script()
        {
        }
        #region methods
        /// <summary>
        /// //Method used to run new background thread to prevent script window from freezing while execution
        /// </summary>
        private void RunOnNewStaThread(Action action)
        {
            Thread thread = new Thread(() => action());
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
        /// <summary>
        /// Method used to initialize main window and set its context to the MainViewModel
        /// </summary>
        private void InititializeAndStartMainWindow(EsapiWorker esapiWorker)
        {

            mainViewModel = new IsoSplitViewModel(esapiWorker);
            Logger.LogInfo("MainViewModel set");

            //Instead of hooking DataContext to one specific ViewModel, we use a MainViewModel which allows navigation between different View Models.
            //It can be useful for projects with several Views as the navigation layer is required

            mainWindow = new ScriptWindow() { DataContext=mainViewModel};
            mainWindow.ShowDialog();
        }

        


        #endregion

        //[MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            scriptContext = context;
            // Rather than take the standard ESAPI input window, this code instantiates a WPF window subclass (ScriptWindow) which can be edited using the VS Designer 

            // If you don't want to use a GUI you can just pass the relevant ScriptContext fields to any method you want and call this method
            // in the PluginTesterForm.cs instead of creating the ScriptWindow

            // The developer recommended you design your script so that you don't do any work in the Execute method,
            // or any method which uses the "ScriptContext" class directly, as this class isn't available in a standalone application.  

            // Instead, have Execute call a method that takes elements you can get from either the standalone or scriptcontext (like Patient and PlanSetup)
            // so you can call (and debug) it from the standalone PluginTester without having to make any changes
            // to the Script class itself.

            //Start logger
            Logger.Initialize(context.CurrentUser.Name);
            Logger.LogInfo("Logger has been initialized");

            //Start EsapiWorker for a patient and structureSet from context in the main thread
            var esapiWorker = new EsapiWorker(context.Patient, context.StructureSet);
            Logger.LogInfo("EsapiWorker has been initialized");

            //Queue of tasks that prevents the script from exiting until the window is closed
            DispatcherFrame frame = new DispatcherFrame();
            Logger.LogInfo("Dispatcher frame started");

            //This method won't return until the window is closed
            RunOnNewStaThread(() => 
            { 
                InititializeAndStartMainWindow(esapiWorker);
                //mainViewModel.Dispose();
                frame.Continue = false; });
            Logger.LogInfo("New thread started");
            
            //Start the new queue, waiting until the window is closed
            Dispatcher.PushFrame(frame);
            Logger.LogInfo("New queue started");

            // Note that Eclipse will pass you a handle to a window if you want to configure it yourself at runtime by adding and formating the layout of the controls.
            // The developer finds this to be very tedious and prefer to design the GUI using the Visual Studio designer.  However, this limits you (well, without adding significant complexity)
            // to running your script as a DLL (i.e. you have to compile the ScriptTemplate project to get ScriptTemplate.esapi.dll and run this in Eclipse.
            // If you don't want to do this, see https://github.com/VarianAPIs/Varian-Code-Samples/blob/master/Eclipse%20Scripting%20API/plugins/DvhLookups.cs for some examples of configuring the window layout


        }
    }
}
