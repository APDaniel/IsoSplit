using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using IsoSplitProject;
using IsoSplitProject.Helpers;
using IsoSplitProject.ViewModels;
using VMS.TPS.Common.Model.API;




namespace PluginTester
{
    public partial class PluginTesterForm : Form
    {
        VMS.TPS.ESAPIApplication app;

        private void RunOnNewStaThread(Action a)
        {
            Thread thread = new Thread(() => a());
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public PluginTesterForm()
        {
            InitializeComponent(); // This is the entry point to the standalone app that you will use to debug your script
            app = VMS.TPS.ESAPIApplication.Instance; // instantiate the ESAPI context

            // Can use this to seed the form if you have a standard test patient.
            textBox_PID.Text = "VC_TBI_9";
            textBox_SSID.Text = "CT1_TotalBody";
            textBox_planId.Text = "Plan3_2C";
            textBox_CourseID.Text = "Course1_planning";
        }


        private void InitializeAndStartMainWindow(EsapiWorker esapiWorker)
        {
            var mainViewModel = new IsoSplitViewModel(esapiWorker);
            try
            {
                var mainWindow = new ScriptWindow() { DataContext = mainViewModel};
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                var test = ex.Message;
            }

        }

        private async void Run_Script_Click(object sender, EventArgs e)
        {
            string PID = textBox_PID.Text;  // get the patient ID
            string CourseID = textBox_CourseID.Text;  // get the patient ID
            string PlanID = textBox_planId.Text;
            string SSID = textBox_SSID.Text; // get the plan ID
            Patient patient = null;
            Course course = null;
            PlanSetup planSetup = null;
            StructureSet structureSet = null;
            try
            {
                patient = app.Context.OpenPatientById(PID);
                if (patient == null)
                {
                    System.Windows.Forms.MessageBox.Show("Patient not found!");
                    app.Context.ClosePatient();
                    return;
                }
                course = patient.Courses.FirstOrDefault(x => x.Id == CourseID);
                if (course != null)
                {
                    planSetup = course.PlanSetups.FirstOrDefault(x => x.Id == PlanID);

                }
                structureSet = patient.StructureSets.FirstOrDefault(x => string.Equals(x.Id, SSID, StringComparison.OrdinalIgnoreCase));
                if (structureSet == null)
                {
                    System.Windows.Forms.MessageBox.Show("Structure set not found!");
                    app.Context.ClosePatient();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("There was an error opening the patient / plan.");
                System.Windows.Forms.MessageBox.Show(string.Format("{0} \r\n {1} \r\n {2} \r\n", ex.Message, ex.InnerException, ex.StackTrace));
                app.Context.ClosePatient();
                return;
            }
            try
            {
                EsapiWorker ew = null;
                if (planSetup != null)
                    ew = new EsapiWorker(patient, planSetup);
                else if (structureSet != null)
                    ew = new EsapiWorker(patient, structureSet); // write enabled for plan
                if (ew != null)
                {
                    Logger.Initialize(app.Context.CurrentUser.Id);
                    DispatcherFrame frame = new DispatcherFrame();
                    RunOnNewStaThread(() =>
                    {
                        
                        // This method won't return until the window is closed
                        InitializeAndStartMainWindow(ew);

                        // End the queue so that the script can exit
                        frame.Continue = false;
                    });
                    Dispatcher.PushFrame(frame);

                    if (checkBox_save.Checked)
                        app.Context.SaveModifications();

                    app.Context.ClosePatient();
                }
                else
                    System.Windows.Forms.MessageBox.Show("The plan or structure set was not found.");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("There was an error running your script.");
                System.Windows.Forms.MessageBox.Show(string.Format("{0} \r\n {1} \r\n {2} \r\n", ex.Message, ex.InnerException, ex.StackTrace));
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            app.Context.Dispose();
        }

    }
}
