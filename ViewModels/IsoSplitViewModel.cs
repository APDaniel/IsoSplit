using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using IsoSplit.Models;
using IsoSplitProject.Commands;
using IsoSplitProject.Helpers;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;


namespace IsoSplitProject.ViewModels
{

    /// <summary>
    /// The primary view model. Represents logic for isocenter splitting: copies the source plan for each iso and deletes excessive isos
    /// </summary>

    public class IsoSplitViewModel : BaseViewModel
    {
        #region Private fields
        private List<BeamToPlanID_Model> _regionrules { get; set; }
        private string _regionRulesXmlPath { get; set; }
        private int _isocenterGroupNumber = 1;
        /// <summary>
        /// private string used in script logic for
        /// defining course where all the FIN plans are saved. 
        /// May be customized by used in settings view
        /// </summary>
        private string _finCourseID { get; set; } = "Course1";

        /// <summary>
        /// private boolean used in script logic for selecting when the settings elements are visible to user.
        /// A crude approach choosen instead of adding another view and viewModel navigation layer for simplicity.
        /// </summary>
        private bool _areSettingsVisible { get;set; }

        /// <summary>
        /// private string used in script logic for specifying 
        /// to which isocenter add CBCT imaging.
        /// By default set to 'Chest'
        /// </summary>
        private string _selectedPriorityMatchIso { get; set; } = "Chest"; //Default value

        /// <summary>
        /// private boolean used in script logic to determine
        /// whether a DoNotTreat plan should be created
        /// </summary>
        private bool _createDoNotTxPlanCheckBoxResult { get; set; } = true;
        /// <summary>
        /// private boolean used in script logic to determine
        /// whether there is one or two plans for processing
        /// </summary>
        private bool _singlePlanCheckBoxResult { get; set;} = false;

        /// <summary>
        /// private string used in script logic to determine
        /// which plan to use as a primary source plan.
        /// In case of board rotation, usually HF, because
        /// specified in UI as HF. But can be HF or FF. 
        /// </summary>
        private string _selectedRO2CCoursePlanID { get; set; }

        /// <summary>
        /// private string used in script logic to determine
        /// which plan to use as a secondary source plan.
        /// In case of board rotation, usually FF, because
        /// specified in UI as FF. But can be HF or FF. 
        /// </summary>
        private string _selectedFFCoursePlanID { get; set; }

        /// <summary>
        /// private string used in script logic to determine
        /// patient Id. Displayed in UI as a sanity check, but
        /// not critical. Binary plugins are loaded from context anyway.
        /// </summary>
        private string _patientId { get; set; } = "Design value"; //design value used for debugging purposes only

        /// <summary>
        /// private string used in script logic to determine
        /// which progress report phrase to show
        /// </summary>
        private string _spinnerPhrase { get; set; } = "Spinner phrase";

        /// <summary>
        /// private string used in script logic to determine
        /// which progress comment phrase to show
        /// </summary>
        private string _spinnerPhrase1 { get; set; } = "Spinner phrase1";

        /// <summary>
        /// private bool used in script logic to determine
        /// if a spinner should be shown
        /// </summary>
        private bool _workIsInProgress { get; set; } = false;

        /// <summary>
        /// private ObservableCollection used in script logic to determine
        /// the collection of potential naming conventions for priority match isocenters.
        /// Used in UI
        /// </summary>
        private ObservableCollection<IsoItemModel> _collectionOfPriorityMatchIso { get; set; } =
            new ObservableCollection<IsoItemModel> {};

        /// <summary>
        /// private ObservableCollection used in script logic to determine
        /// the collection of available plan IDs for the course loaded in context.
        /// When used in PluginTester, populates all plans for the patient due
        /// to the differences between standalone and binary plugins
        /// </summary>
        private ObservableCollection<string> _availablePlanIDs { get; set; } = new ObservableCollection<string> {};

        /// <summary>
        /// private bool used in script logic to determine
        /// if the Execute button should be available for the user
        /// </summary>
        private bool _isButtonEnabled { get; set; } = false;
        #endregion
        #region Public fields

        /// <summary>
        /// public string used in script logic to determine
        /// final course id. Automatically trimmed to 8 characters
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string finCourseID 
        { 
            get => _finCourseID;
            set  
            {
                if (value != null && value.Length > 16)
                    value = value.Substring(0, 16);
                if (value.Length < 1)
                    value = "Course";
                _finCourseID = value;
            }
        }

        /// <summary>
        /// public bool used in script logic to determine
        /// if the settings elements should be shown to the user.
        /// A crude approach choosen instead of adding another view and viewModel navigation layer for simplicity.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public bool areSettingsVisible
        {
            get => _areSettingsVisible;
            set => _areSettingsVisible = value;
        }

        /// <summary>
        /// public string used to specify
        /// priority match isocenter to which CBCT imaging is added
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string selectedPriorityMatchIso
        {
            get => _selectedPriorityMatchIso;
            set => _selectedPriorityMatchIso = value; 
        }

        /// <summary>
        /// public bool used to specify
        /// if a DoNotTreat maln should be created.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public bool createDoNotTxPlanCheckBoxResult 
        { 
            get => _createDoNotTxPlanCheckBoxResult; 
            set => _createDoNotTxPlanCheckBoxResult = value; 
        }

        /// <summary>
        /// public boolean used in script logic to determine
        /// whether there is one or two plans for processing
        /// </summary>
        public bool singlePlanCheckBoxResult 
        {
            get => _singlePlanCheckBoxResult;
            set  
                {
                    _singlePlanCheckBoxResult = value;
                    UpdateButtonState();
                    PropagateIsocentersForCBCTselection();
                }
        }

        /// <summary>
        /// Esapi worker used to enable patient record data
        /// calls with multithreading
        /// </summary>
        public EsapiWorker esapiWorker;

        /// <summary>
        /// Dispatcher is used to enable multithreading
        /// </summary>
        public Dispatcher userInterface = null;

        /// <summary>
        /// public string used to specify
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string patientId { get => _patientId; set => _patientId = value; }

        /// <summary>
        /// public ObservableCollection used to specify
        /// collection of priority match isocenters.
        /// That collection is hard-coded in a private field, but with get set
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public ObservableCollection<IsoItemModel> collectionOfPriorityMatchIso
        {
            get => _collectionOfPriorityMatchIso;
            set => _collectionOfPriorityMatchIso = value;
        }

        /// <summary>
        /// public ObservableCollection used to specify
        /// the list of available plans for the selected patient.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public ObservableCollection<string> availablePlanIDs 
        { 
            get=>_availablePlanIDs;
            set => _availablePlanIDs = value;
        }

        /// <summary>
        /// public string used to specify
        /// which plan to use as a primary source plan.
        /// When the value is changed: 
        /// 1.attempts to autopopulate
        /// FF plan picking the first plan with 'FF' in its ID.
        /// 2. Updates execute button availability
        /// 3. Updates the final course ID.
        /// In case of board rotation, usually HF, because
        /// specified in UI as HF. But can be HF or FF. 
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string selectedRO2CPlanID
        { 
            get  => _selectedRO2CCoursePlanID;
            set 
            { 
                _selectedRO2CCoursePlanID = value; 
                AutoPopulateFFPlanSelection();
                UpdateButtonState();
                UpdateFinCourseID();
                PropagateIsocentersForCBCTselection();
            } 
        }

        /// <summary>
        /// public string used to specify
        /// which plan to use as a primary source plan.
        /// When the value is changed: 
        /// 1. Updates execute button availability
        /// In case of board rotation, usually FF, because
        /// specified in UI as FF. But can be HF or FF. 
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string selectedFFPlanID 
        { 
            get => _selectedFFCoursePlanID; 
            set 
            { 
                _selectedFFCoursePlanID = value;
                UpdateButtonState();
                PropagateIsocentersForCBCTselection();
            }
        }

        /// <summary>
        /// public string used to specify
        /// which spinner report phrase to show.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string spinnerPhrase { get => _spinnerPhrase; set => _spinnerPhrase = value; }

        /// <summary>
        /// public string used to specify
        /// which spinner report comment to show.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public string spinnerPhrase1 { get => _spinnerPhrase1; set => _spinnerPhrase1 = value; }


        /// <summary>
        /// public boolean used to specify
        /// if the spinner should be shown.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public bool workIsInProgress { get => _workIsInProgress;set=> _workIsInProgress=value; }


        /// <summary>
        /// public bool used to determine
        /// if the execute button is enabled.
        /// Used as a binding layer between a private field in script logic
        /// and UI
        /// </summary>
        public bool isButtonEnabled { get => _isButtonEnabled; set => _isButtonEnabled = value; }
        #endregion
        #region Public commands

        /// <summary>
        /// public ICommand used in UI to run SplitIsoCommand.
        /// Used as a connection layer between the script logic and UI.
        /// </summary>
        public ICommand CommandSplitIsoCommand { get; set; }

        /// <summary>
        /// public ICommand used in UI to run OpenSettingsCommand
        /// Used as a connection layer between the script logic and UI.
        /// </summary>
        public ICommand CommandOpenSettingsCommand { get; set; }

        /// <summary>
        /// public ICommand used in UI to run OpenSettingsCommand
        /// Used as a connection layer between the script logic and UI.
        /// </summary>
        public ICommand CommandApplyFinCorseCommand { get; set; }
        #endregion
        #region Constructor
        public IsoSplitViewModel(EsapiWorker _esapiWorker) //Initialize defined view model, set non-freezing user interface
        {
            esapiWorker = _esapiWorker;
            userInterface = Dispatcher.CurrentDispatcher;

            #region Initialize commands
            CommandSplitIsoCommand = new RelayCommand(CommandSplitIso);
            CommandOpenSettingsCommand = new RelayCommand(CommandOpenSettings);
            CommandApplyFinCorseCommand = new RelayCommand(CommandApplyFinCorse);
            #endregion

            #region Run default methods to lookup pt data, etc...
            LookupPatientData();
            #endregion

        }
        #endregion
        #region Private methods
        /// <summary>
        /// private void designed to be used as a trigger when selection in finCourseID textblock
        /// is appied.
        /// </summary>
        private void CommandApplyFinCorse() 
        {
            Logger.LogInfo("Command called to update fin course ID");
            _finCourseID = _finCourseID;
        }

        /// <summary>
        /// private void designed to be used as command to change
        /// the value of finCourseID based on the selection in the
        /// TextBox UI in Settings layout
        /// </summary>
        private void UpdateFinCourseID()
        {
            Logger.LogInfo("Method called to update the FIN course ID");
            if (_selectedRO2CCoursePlanID != null)
            {
                _finCourseID = _selectedRO2CCoursePlanID.Split('/')[0];
                _finCourseID = _selectedRO2CCoursePlanID.Split('_')[0];

                if (_finCourseID.Length > 8) 
                {
                    Logger.LogInfo($"Course ID {_finCourseID} exceeds 8 characters. Trimming to 8: {_finCourseID.Substring(0, 8)}");
                    _finCourseID = _finCourseID.Substring(0, 8); 
                }

                if (!_finCourseID.Contains("Course1"))
                {
                    Logger.LogInfo($"Course {_finCourseID} does not contain 'Course1', adjusting naming conventions...");
                    var digits = new string(_finCourseID.Where(char.IsDigit).ToArray());
                    int numericSuffix = 1;

                    if (!string.IsNullOrEmpty(digits))
                        int.TryParse(digits, out numericSuffix);

                    _finCourseID = "Course" + numericSuffix;
                    Logger.LogInfo($"Course naming conventions changed to: {_finCourseID}");
                }
            }
        }

        /// <summary>
        /// private void used to if the execute button
        /// is enabled based on the selection of RO2C and FF plans
        /// </summary>
        private void UpdateButtonState()
        {
            _isButtonEnabled =
                _singlePlanCheckBoxResult
                ? !string.IsNullOrEmpty(_selectedRO2CCoursePlanID)
                : !string.IsNullOrEmpty(_selectedRO2CCoursePlanID)
                && !string.IsNullOrEmpty(_selectedFFCoursePlanID);

            bool _bothPlansSelected = 
                !string.IsNullOrEmpty(_selectedRO2CCoursePlanID)
                && !string.IsNullOrEmpty(_selectedFFCoursePlanID);

            if (_bothPlansSelected&&_singlePlanCheckBoxResult)
            {
                userInterface.Invoke(() =>
                { 
                    _selectedFFCoursePlanID = null;
                    _bothPlansSelected = false;
                });
            }    
        }

        ///<summary>
        ///Command to split isocenters into separate plans
        /// </summary>
        private async void CommandSplitIso()
        {
            Logger.LogInfo("Method called to split isocenters");
            try
            {
                await esapiWorker.AsyncRunStructureContext((_patient, _structureSet) =>
                {
                    //Allow ESAPI to make overrides in case data
                    Logger.LogInfo("Begin modifications");
                    _patient.BeginModifications();

                    Course courseToSaveCopiedPlan = _patient.Courses.FirstOrDefault(c => c.Id.Equals(_finCourseID, StringComparison.OrdinalIgnoreCase));
                    
                    //Check if the fin course exists and create a new one if it is not
                    if (courseToSaveCopiedPlan==null)
                    {
                        Logger.LogInfo($"Course to save copied plans {_finCourseID} is empty. Creating a new course");
                        courseToSaveCopiedPlan =_patient.AddCourse();
                        courseToSaveCopiedPlan.Id = _finCourseID;
                        Logger.LogInfo($"Course {courseToSaveCopiedPlan.Id} added to case# {_patient.Id}");
                    }
                    else { Logger.LogInfo($"Course to save copied plans defined as: {courseToSaveCopiedPlan.Id}"); }

                    //Call method to split isocenters for HF sourse plan
                     Logger.LogInfo($"Calling method to split isocenters for the HF plan {_selectedRO2CCoursePlanID}");
                    if ((_selectedRO2CCoursePlanID != null)&& (_singlePlanCheckBoxResult == false)) 
                        SplitPlanByIso(_patient, _selectedRO2CCoursePlanID, "HF", courseToSaveCopiedPlan);

                    //Call a method to split isocenters for FF sourse plan
                    Logger.LogInfo($"Calling method to split isocenters for the FF plan {_selectedFFCoursePlanID}");
                    if ((_selectedFFCoursePlanID != null)&& (_singlePlanCheckBoxResult == false)) 
                        SplitPlanByIso(_patient, _selectedFFCoursePlanID, "FF", courseToSaveCopiedPlan);

                    //Call a method to split isocenters for 2C sourse plan
                    Logger.LogInfo($"Calling method to split isocenters for the 2C plan {_selectedRO2CCoursePlanID}");
                    if (_singlePlanCheckBoxResult == true) 
                        SplitPlanByIso(_patient, _selectedRO2CCoursePlanID, "2C", courseToSaveCopiedPlan);
                        
                    if (_createDoNotTxPlanCheckBoxResult == true)
                    {
                        Logger.LogInfo("Checkbox 'Create DoNotTreatPlan' checked. Adding the SGRT plan");
                        _spinnerPhrase1 = "";
                        _spinnerPhrase = $"Creating a DoNotTreatPlan...";

                        Course course = _patient.Courses.FirstOrDefault(c=>c.Id.Equals(_finCourseID,StringComparison.OrdinalIgnoreCase));
                        Logger.LogInfo($"DoNotTreat plan is being saved to course: {course.Id}");

                        Course sourceCourse = _patient.Courses.FirstOrDefault
                        (c => c.ExternalPlanSetups.Any(p => p.Id.Equals(_selectedRO2CCoursePlanID.Split('/')[1], StringComparison.OrdinalIgnoreCase)));
                        Logger.LogInfo($"DoNotTreat plan source course is: {sourceCourse.Id}");

                        ExternalPlanSetup sourcePlan = sourceCourse.ExternalPlanSetups.FirstOrDefault
                        (sp => sp.Id.Equals(_selectedRO2CCoursePlanID.Split('/')[1], StringComparison.OrdinalIgnoreCase));
                        Logger.LogInfo($"DoNotTreat plan source plan is: {sourcePlan.Id}");

                        CreateDoNotTxPlan(sourcePlan, course);
                    }
                    else { Logger.LogInfo("Checkbox 'Create DoNotTreatPlan' is not checked. Skipping adding the SGRT plan"); }
                    
                    _spinnerPhrase1 = "";
                    _spinnerPhrase = $"Attempting to add reference points...";
                    CreateRegionReferencePoints(_patient);

                    _spinnerPhrase1 = "";
                    Logger.LogInfo("All done. Terminating sequence...");
                    _spinnerPhrase = $"Plans created successfully!\nThe script will exit in 5 sec";
                    Thread.Sleep(5000);
                    _workIsInProgress = false;
                });
            }
            catch (Exception exception)
            {
                //Log any appeared issues
                Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                System.Windows.MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
            }
        }

        ///<summary>
        ///Method to lookup patient data (available plans, pt ID, available structure sets)
        /// </summary>
        public async void LookupPatientData()
        {
            _availablePlanIDs.Clear();
            Logger.LogInfo("Method called to pull pt data");
            try
            {
                await esapiWorker.AsyncRunStructureContext((_patient, _structureSet) =>
                {
                    _patientId = _patient.Id;
                    foreach (var course in _patient.Courses)
                    {
                        foreach (var plan in course.ExternalPlanSetups)
                        {
                            string _planIDtoAdd = course.Id+"/"+plan.Id;
                            Logger.LogInfo($"Adding planID {_planIDtoAdd} to the list of available plans");
                            userInterface.Invoke(() => 
                            { 
                                _availablePlanIDs.Add(_planIDtoAdd); 
                            });
                            
                        }
                    }
                });
                Logger.LogInfo("Ensuring alphabetical order of the observable collections with available plan IDs");
                _availablePlanIDs.OrderBy(x => x); //Ensure alphabetic order
                _regionRulesXmlPath= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                _regionRulesXmlPath = Path.Combine(_regionRulesXmlPath, "XMLconfigFiles", "BeamToPlanID_Rules.xml");
                if (!File.Exists(_regionRulesXmlPath))
                {
                    Logger.LogError($"XML scheme file was not found at {_regionRulesXmlPath}");
                    throw new FileNotFoundException($"XML scheme file was not found at {_regionRulesXmlPath}");
                }
                _regionrules = new List<BeamToPlanID_Model>();
                _regionrules = LoadRegionRules(_regionRulesXmlPath);
                AutoPopulatePlanSelection();
                AutoPopulateFFPlanSelection();
                UpdateButtonState();
                UpdateFinCourseID();
                PropagateIsocentersForCBCTselection();
                    
            }
            catch (Exception exception)
            {
                //Log any appeared issues
                Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                System.Windows.MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
            }
        }

        /// <summary>
        /// Method to generate plan ID under the limit of characters
        /// </summary>
        string MakeSafeId(string s, Course course)
        {
            Logger.LogInfo($"Method called to check and create safe ID for the Plan: {s} in the Course: {course}");
            //create a baseID string
            s = new string(s.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            if (s.Length>12) s=s.Substring(0,12);
            string baseID = s;
            Logger.LogInfo($"BaseID selected as: {baseID}");

            //check if there is a number at the end
            int numericSuffix = 0;
            int i = baseID.Length - 1;
            while (i >= 0 && char.IsDigit(baseID[i])) i--;
            if (i != baseID.Length - 1)
            {
                Logger.LogInfo($"Detected number at the end of ID: {baseID}");
                string numberPart=baseID.Substring(i+1);
                Logger.LogInfo($"Numeric part is: {numberPart}");
                baseID = baseID.Substring(0,i + 1);
                numericSuffix=int.Parse(numberPart);
            }

            string candidate = s;

            Logger.LogInfo($"Checking if there is any plan with id: {s} present in the course: {course}");
            while (course.ExternalPlanSetups.Any(p => p.Id.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
            {   Logger.LogInfo($"Plan with id: {s} detected in the course: {course}"); 
                numericSuffix++; 
                candidate = baseID + numericSuffix;
                if (numericSuffix == 99) 
                { 
                    Logger.LogInfo($"Epic fail. Numeric suffix is exceeding 99. Falling back to ID: {candidate}"); 
                    return candidate; 
                }
            }

            return candidate;
        }

        /// <summary>
        /// Method to copy plan
        /// </summary>
        public void SplitPlanByIso(Patient _patient, string _selectedPlanID, string indexHForFFor2C/*Please define as HF or FF or 2C(in case there is a single plan)*/, Course courseToSaveCopiedPlan)
        {
            Logger.LogInfo($"Method called to split {indexHForFFor2C} plan {_selectedPlanID} for case #{_patient.Id} and save FIN plans to course: {courseToSaveCopiedPlan}");
            try
            {
                _workIsInProgress=true;
                _spinnerPhrase = "Initiating algorithm...";
                _spinnerPhrase1 = $"... for {indexHForFFor2C} plan";
                Thread.Sleep(2000);
                //pull up the plan
                Logger.LogInfo("Pulling up the plan ID");
                var coursePlanID = _selectedPlanID.Split('/');
                Logger.LogInfo($"Plan name is: {_selectedPlanID}");
                _spinnerPhrase = $"Loading plan ID: {_selectedPlanID}...";

                var course = _patient.Courses.FirstOrDefault(c => c.Id == coursePlanID[0]);
                Logger.LogInfo($"Course identified as: {coursePlanID[0]}");
                _spinnerPhrase = $"Loading course: {coursePlanID[0]}...";

                Logger.LogInfo("Pulling up the External Plan Setup");
                ExternalPlanSetup plan = course.ExternalPlanSetups.First(p => p.Id == coursePlanID[1]);
                var structureSet = (StructureSet)plan.StructureSet;
                var userOrigin = structureSet.Image.UserOrigin;
                Logger.LogInfo($"External Plan Setup is {plan.Id}");
                _spinnerPhrase = $"Loading plan: {plan.Id}...";


                Logger.LogInfo("Defining keys for isocenters");
                _spinnerPhrase = $"Initiating iso split logic...";
                //define isocenter as keys
                string isoKey(Beam b)
                {
                    var v = b.IsocenterPosition;
                    Logger.LogInfo($"Isocenter captured: {Math.Round(v.x- userOrigin.x)}|{Math.Round(v.y-userOrigin.y)}|{Math.Round(v.z-userOrigin.z)} for beam {b.Id}");
                    return $"{Math.Round(v.x)}|{Math.Round(v.y)}|{Math.Round(v.z)}|";
                }

                //Define isocenter groups and save them into a list
                _spinnerPhrase = $"Looking into isocenter groups...";
                Logger.LogInfo("Defining isocenter groups and saving them into a list");
                bool isHF = string.Equals(indexHForFFor2C, "HF", StringComparison.OrdinalIgnoreCase);
                bool isFF = string.Equals(indexHForFFor2C, "FF", StringComparison.OrdinalIgnoreCase);
                bool is2C = string.Equals(indexHForFFor2C, "2C", StringComparison.OrdinalIgnoreCase);

                var groups = plan.Beams.Where(b => 
                !b.IsSetupField&&((isHF && b.IsocenterPosition.z-userOrigin.z >= 0)||(isFF && b.IsocenterPosition.z-userOrigin.z < 0) ||(is2C)))
                    .GroupBy(isoKey).ToList();
                //Run the split beams logic
                Logger.LogInfo("Starting the split beam logic");
                int idx = 1;
                foreach (var g in groups)
                {
                    string spinnerComment = "";
                    if (_selectedRO2CCoursePlanID != _selectedFFCoursePlanID)
                        spinnerComment = indexHForFFor2C;

                    _spinnerPhrase1 = $"... for iso# {idx} {spinnerComment}";
                    _spinnerPhrase = $"Initiating iso split logic...";
                    //Copy plan
                    Logger.LogInfo($"Making a copy of the plan {plan.Id}");
                    ExternalPlanSetup planCopy = (ExternalPlanSetup)courseToSaveCopiedPlan.CopyPlanSetup(plan);

                    //Find the first iso to work with
                    var isoLabel = g.First().IsocenterPosition;
                    Logger.LogInfo($"Initiating logic for iso: X={isoLabel.x}; Y={isoLabel.y}; Z={isoLabel.z};");
                    _spinnerPhrase = $"Labeling isocenters...";

                    //Delete beams that do not belong to the selected group
                    Logger.LogInfo("Deleting beams that do not belong to the current iso group");
                    var keep = new HashSet<string>(g.Select(b => b.Id));
                    foreach (var b in planCopy.Beams.ToList())
                    {
                        Logger.LogInfo($"Checking field {b.Id}");
                        if (!keep.Contains(b.Id))
                        {
                            Logger.LogInfo($"Removing field {b.Id} in {planCopy.Id}");
                            _spinnerPhrase = $"Removing beam {b.Id}...";
                            planCopy.RemoveBeam(b);
                        }
                    }
                    idx++;

                    //Define copy plan ID
                    string prefix = _collectionOfPriorityMatchIso.FirstOrDefault(x=>x.groupNumber == _isocenterGroupNumber)?.isocenterID;
                    string baseID = prefix + "FIN";
                    planCopy.Id = MakeSafeId(baseID, courseToSaveCopiedPlan);
                    Logger.LogInfo($"Plan copy ID defined as: {planCopy.Id}");
                    _spinnerPhrase = $"Defining copied plan ID {planCopy.Id}...";
                    Thread.Sleep(2000);

                    //Add imaging beams
                    _spinnerPhrase = $"Adding setup beams...";
                    AddSetupFields(planCopy, double.NaN); //Add CBCT
                    AddSetupFields(planCopy, 0.0); //Add kV G0
                    AddSetupFields(planCopy, 90.0); //Add kV G90
                    AddSetupFields(planCopy, 180.0); //Add kV G180  

                    _isocenterGroupNumber++;
                }
            }

            catch (Exception exception)
            {
                //Log any appeared issues
                _spinnerPhrase = $"Oops... something went wrong...";
                Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                System.Windows.MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                _workIsInProgress = false;
            }
        }

        /// <summary>
        /// Method to push reference points without location to the plan open in context
        /// </summary>
        /// <param name="plan"></param>
        private void CreateRegionReferencePoints(Patient patient)
        {
            Logger.LogInfo($"Method called to add reference points to the RO2C plan open in context: {_selectedRO2CCoursePlanID} for case #{patient.Id}");
            string rpID="";
            try
            {
                string courseID = _selectedRO2CCoursePlanID.Split('/')[0];
                Logger.LogInfo($"Source courseID defined is: {courseID}");

                string planID = _selectedRO2CCoursePlanID.Split('/')[1];
                Logger.LogInfo($"Source planID defined is: {planID}");

                Course course = patient.Courses.FirstOrDefault(b => b.Id.Equals(courseID,StringComparison.OrdinalIgnoreCase));
                Logger.LogInfo($"Pulled up source course: {course.Id}");

                ExternalPlanSetup plan = course.ExternalPlanSetups.FirstOrDefault(b => b.Id.Equals(planID, StringComparison.OrdinalIgnoreCase));
                Logger.LogInfo($"Pulled up source plan: {plan.Id}");

                var location = new VVector(0, 0, 0);
                var regionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (_createDoNotTxPlanCheckBoxResult == true) 
                {
                    Logger.LogInfo($"Create DoNotTx plan checkbox is checked. No SGRT region code found. Updating the region codes set");
                    regionCodes.Add("SGRT"); 
                }

                foreach (var beam in plan.Beams.Where(b => !b.IsSetupField))
                {
                    Logger.LogInfo($"Pulling beam ID {beam.Id} for region code");
                        var id = beam.Id ?? string.Empty;
                        //var lettersOnly = new string(id.TakeWhile(char.IsLetter).ToArray());
                        if (string.IsNullOrEmpty(id))
                            continue;

                        var region = SelectPlanOrRpIDBasedOnInputString(id);
                        if (string.IsNullOrEmpty(region))
                            region = "Plan";

                        if (!regionCodes.Add(region))
                            continue;
                    Logger.LogInfo($"Added region code: {region}");
                }
                foreach (var region in regionCodes)
                {
                    Logger.LogInfo("Looping through region codes");
                    string baseID = (string)region + "_NP";
                    rpID = baseID;
                    if (patient.ReferencePoints.Any(rp => !rp.Id.Equals(rpID, StringComparison.OrdinalIgnoreCase)))
                    {
                        Logger.LogInfo($"Reference point with ID: {rpID} not found in case#{patient.Id}. Updating IEnumerable with reference points.");
                        var refPt = patient.AddReferencePoint(true, rpID);

                        _spinnerPhrase = $"Adding reference point {rpID}...";
                        _spinnerPhrase1 = $"...for case# {patient.Id}";
                        Thread.Sleep(2000);
                        if (refPt.Id != "SGRT_NP")
                        {
                            //Define Ref Point dose limits
                            Logger.LogInfo($"Setting dose limits for ref point {refPt.Id}");
                            refPt.SessionDoseLimit = plan.DosePerFraction;
                            refPt.DailyDoseLimit = 2 * refPt.SessionDoseLimit;
                            refPt.TotalDoseLimit = (DoseValue)(plan.DosePerFraction * plan.NumberOfFractions);
                            Logger.LogInfo($"Dose limits for {refPt.Id} are set as: " +
                                $"Session={refPt.SessionDoseLimit}; Daily={refPt.DailyDoseLimit}; Total={refPt.TotalDoseLimit}");
                        }
                        else
                        {
                            //Define SGRT Point dose limits
                            Logger.LogInfo($"Setting dose limits for ref point {refPt.Id}");
                            refPt.SessionDoseLimit = new DoseValue(1.0, DoseValue.DoseUnit.cGy);
                            refPt.DailyDoseLimit = 2 * refPt.SessionDoseLimit;
                            refPt.TotalDoseLimit = (DoseValue)(refPt.DailyDoseLimit * plan.NumberOfFractions);
                            Logger.LogInfo($"Dose limits for {refPt.Id} are set as: " +
                                $"Session={refPt.SessionDoseLimit}; Daily={refPt.DailyDoseLimit}; Total={refPt.TotalDoseLimit}");
                        }

                        _spinnerPhrase = $"Setting dose limits ...";
                        _spinnerPhrase1 = $"... for ref point {refPt.Id}";
                        Thread.Sleep(2000);
                    }
                    else { Logger.LogInfo($"RP {rpID} exists. Ignore."); _spinnerPhrase1 = ""; }
                }
            }
            catch { Logger.LogError($"Something went wrong when creating a reference point {rpID}. Try to open RO2C plan in context. Bypassed."); }
            _spinnerPhrase1 = "";
        }

        /// <summary>
        /// This method pulls HF, FF plans in the the UI
        /// </summary>
        private void AutoPopulateFFPlanSelection()
        {
                if (_availablePlanIDs != null&&_selectedRO2CCoursePlanID!=null)
                {
                    var _selectedCourse = _selectedRO2CCoursePlanID.Split('/')[0];
                    var _sameCoursePlanIDs = new ObservableCollection<string>();
                    foreach (var item in _availablePlanIDs)
                    {
                        if (item.Split('/')[0].Equals(_selectedCourse, StringComparison.OrdinalIgnoreCase) == true)
                        { _sameCoursePlanIDs.Add(item); }
                    else { _selectedFFCoursePlanID = null; }
                    }
                    _selectedFFCoursePlanID = _sameCoursePlanIDs.FirstOrDefault(plan => plan.ToLower().Contains("ff"));
                }
        }

        /// <summary>
        /// private method used to autopopulate the FF plan by looking into plan IDs and selecting the first one with "RO2C" in its ID
        /// </summary>
        private void AutoPopulatePlanSelection() 
        {
            Logger.LogInfo("Method called to populate RO2C plan");
            if (_availablePlanIDs != null)
            {
                _selectedRO2CCoursePlanID = _availablePlanIDs.FirstOrDefault(
                    plan => plan.IndexOf("RO2C",StringComparison.OrdinalIgnoreCase)>=0
                    ||plan.Contains("2C"));
            }
        }

        /// <summary>
        /// This method creates ref point ID from field name. Helpful for standalone apps.
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        private object CreateRefPointIDFromFieldName(ExternalPlanSetup plan)
        {
            Logger.LogInfo($"Called a method to select reference point ID from the first field ID in the plan: {plan}");
            //Begin by reading non-setup field ID and using it for the RP ID
            string field = "";
            try 
            { 
                field = plan.Beams.FirstOrDefault(b => !b.IsSetupField).Id;
                Logger.LogInfo($"Reading field ID: {(string)field}");
            }
            catch { Logger.LogInfo($"Field ID is unreadable. Falling back to 'RP' ID"); }
            if (field == null) field = "RP";

            var baseID = new string(field.Where(char.IsLetter).ToArray());
            baseID = baseID + "_NP";
            if (baseID.Length > 14)
            { Logger.LogInfo("RP ID is longer than 14 char - shortening"); baseID = baseID.Substring(0, 13); }

            //If unique RP ID, then return. If not - add numeric suffix
            if (!plan.ReferencePoints.Any(rp => rp.Id.Equals(baseID, StringComparison.OrdinalIgnoreCase)))
                return baseID;
            int n = 1;
            string candidate;
            do
            {
                candidate = $"{baseID}{n}";
                n++;
            }
            while (plan.ReferencePoints.Any(rp=>rp.Id.Equals(candidate,StringComparison.OrdinalIgnoreCase)));
            return candidate;
        }

        /// <summary>
        /// This method mapps IDs to region-specific isocenters for TBI
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string SelectPlanOrRpIDBasedOnInputString(string input)
        {
            Logger.LogInfo($"Method called to select item ID based on the input: {input}");
            
            if (string.IsNullOrEmpty(input))
            {
                Logger.LogInfo("The input string is empty, falling back to ID: 'Plan'");
                return "Plan"; 
            }

            var s= input.ToLower();
            foreach (var rule in _regionrules) 
            {
                if (ContainsAllChars(s, rule.Pattern))
                {
                    Logger.LogInfo($"The input string is defined as: {rule.Label}");
                    return rule.Label;
                }
                    
            }

            bool ContainsAllChars(string source, string pattern)
            {
                source = source.ToLower();
                pattern = pattern.ToLower();
                return pattern.All(c=>source.Contains(c));
            }
            Logger.LogInfo("The input does not contain characters for Head, Chest, Pelvis, Abdomen, Knees, LegsUp, LegsLo, Legs, or Feet. Falling back to ID: 'Plan'");
            return "Plan";
        }
        /// <summary>
        /// Method to add setup beams
        /// </summary>
        /// <param name="plan">
        /// The <see cref="ExternalPlanSetup"/> Object to which imaging beams will b added
        /// </param>
        /// /// <param name="gantryAngle">
        /// Gantry angle in degrees. If CBCT - should be NaN
        /// </param>
        /// <remarks>
        /// Requires write enabled context and a valid imaging system configured on the treatment
        /// </remarks>
        /// 
        public void AddSetupFields(ExternalPlanSetup plan, double gantryAngle)
        {
            try
            {
                ExternalBeamMachineParameters machineParam;
                VVector planIso;

                //Create machine parameters
                var beam = plan.Beams.FirstOrDefault();
                string energy = beam.EnergyModeDisplayName;
                string fluence = null;
                
                Match EMode = Regex.Match(beam.EnergyModeDisplayName, @"^([0-9]+[A-Z]+)-?([A-Z]+)?", RegexOptions.IgnoreCase);
                if (EMode.Success)
                {
                    if (EMode.Groups[2].Length > 0)
                    {
                        energy=EMode.Groups[1].Value;
                        fluence=EMode.Groups[2].Value;
                    }
                }
                machineParam = new ExternalBeamMachineParameters(beam.TreatmentUnit.Id.ToString(), energy, beam.DoseRate, "STATIC", fluence);
                planIso = beam.IsocenterPosition;

                //Distinguish between CBCT and kV fields by Gantry angle. G is NaN for CBCT
                if (Double.IsNaN(gantryAngle))
                {
                    if (!plan.Beams.Any(bm => bm.Id.Equals("CBCT", StringComparison.OrdinalIgnoreCase)))
                    {
                        var planIsoId = plan.Id.Split('F')[0];
                        if (_collectionOfPriorityMatchIso.Any(
                            i=>i.isSelectedAddCBCT&&
                            string.Equals(i.isocenterID,planIsoId,StringComparison.OrdinalIgnoreCase))
                            ) 
                        {
                            Logger.LogInfo($"Iso for {plan.Id} is defined as priority match. Adding CBCT field");
                            Beam bm = plan.AddSetupBeam(machineParam, new VRect<double>(-80.0, -80.0, 80.0, 80.0), 0.0, 0.0, 0.0, planIso);
                            bm.Id = "CBCT";
                            bm.Name = "CBCT";
                        } 
                    }
                }
                else
                {
                    //Check patient orientation
                    var patientOrientation = plan.TreatmentOrientationAsString;
                    string beamDirection = CheckBeamDirection(gantryAngle, patientOrientation);

                    //Define kV beam Id and Name
                    var beamId= "kV" + beamDirection.ToString();
                    var beamName = "kV" + beamDirection.ToString();
                    Beam bm=null;
                    if (!plan.Beams.Any(b => b.Id.Equals(beamId, StringComparison.OrdinalIgnoreCase)))
                    {
                        bm = plan.AddSetupBeam(machineParam, new VRect<double>(-100.0, -100.0, 100.0, 100.0), 0.0, gantryAngle, 0.0, planIso);
                        //Define kV beam Id and Name
                        bm.Id = beamId;
                        bm.Name = beamName;
                    }
                    else { bm = plan.Beams.FirstOrDefault(b => b.Id.Equals(beamId, StringComparison.OrdinalIgnoreCase)); }
                        //Define DRR parameters
                    var DRRparameters = new DRRCalculationParameters(500, 1.0, 50, 500);
                    bm.CreateOrReplaceDRR(DRRparameters);
                    
                }

                string CheckBeamDirection(double gntrAngle, string ptOrientation)
                {
                    Logger.LogInfo($"Called method to assess kV beam orientation for Gantry: {gntrAngle} and orientation: {ptOrientation}");
                    bool isHF = ptOrientation.Contains("Head First-Supine");
                    if ((gntrAngle >= 315.0 && gntrAngle <= 360.0) || (gntrAngle >= 0.0 && gntrAngle <= 45.0))
                        return "Ant";
                    else if (gntrAngle > 45.0 && gntrAngle < 135.0)
                        return isHF ? "Llat" : "Rlat";
                    else if (gntrAngle >= 135.0 && gntrAngle <= 225.0)
                        return "Post";
                    else if (gntrAngle > 225.0 && gntrAngle < 315.0)
                        return isHF ? "Rlat" : "Llat";
                    else
                        return "";
                }

            }
            catch (Exception exception)
            {
                //Log any appeared issues
                Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                System.Windows.MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
            }

        }

        /// <summary>
        /// private void to be used as a switch
        /// to ahcnge value of the private bool
        /// _areSettingsVisible
        /// </summary>
        public void CommandOpenSettings()
        {
            Logger.LogInfo($"Command called to {(_areSettingsVisible ? "close":"open")} settings window");
            _areSettingsVisible = !_areSettingsVisible;
        }

        /// <summary>
        /// private method used to create the DoNotTreat plan
        /// </summary>
        /// <param name="sourcePlan">Primary plan to make copy from. Usually HF RO2C plan</param>
        /// <param name="course">Course in which DoNotTreat plan is saved. Usually contains all FIN plans</param>
        private void CreateDoNotTxPlan(ExternalPlanSetup sourcePlan, Course course)
        {
            Logger.LogInfo($"Method called to create a DoNotTreat plan from source plan {sourcePlan.Id} and save to {course.Id}");
            string doNoTxPlanID = "DoNotTreat";

            if (!course.ExternalPlanSetups.Any(plan => plan.Id.Equals(doNoTxPlanID, StringComparison.OrdinalIgnoreCase)))
            {
               Logger.LogInfo($"Checked {course.Id}. No {doNoTxPlanID} found. Proceeding with plan creation");
               ExternalPlanSetup doNotTxPlan = (ExternalPlanSetup)course.CopyPlanSetup(sourcePlan);
               var structureSet=(StructureSet)doNotTxPlan.StructureSet;
               var userOrigin = structureSet.Image.UserOrigin;
               Logger.LogInfo($"User origing read as: {userOrigin}");
               Beam lookupBeam = doNotTxPlan.Beams.FirstOrDefault(b => !b.IsSetupField);
               Logger.LogInfo($"Pulling machine parameters from field: {lookupBeam.Id}");

               ExternalBeamMachineParameters ebmp = new ExternalBeamMachineParameters(lookupBeam.TreatmentUnit.Id,"6X",600,"STATIC",null);
                Logger.LogInfo($"Identified: " +
                    $"Machine ID {ebmp.MachineId}; " +
                    $"Energy mode {ebmp.EnergyModeId}; " +
                    $"Dose rate {ebmp.DoseRate}");

                Beam doNotTxBeam = doNotTxPlan.AddStaticBeam(ebmp, new VRect<double>(-100, -100, 100, 100), 0, 0, 0, userOrigin);
               var keepID=doNotTxBeam.Id;

               foreach (var beam in doNotTxPlan.Beams.ToList())
               {
                if (!beam.Id.Equals(keepID,StringComparison.OrdinalIgnoreCase)) 
                        doNotTxPlan.RemoveBeam(beam);
               }
               doNotTxBeam.Id = "DoNotTreat";
               doNotTxPlan.Id = "DoNotTreat";
                Logger.LogInfo($"Beam {doNotTxBeam.Id} added to plan {doNotTxPlan.Id}");
               AddSetupFields(doNotTxPlan, 0);
               AddSetupFields(doNotTxPlan, 90);
               AddSetupFields(doNotTxPlan, 180);
            }
            else { Logger.LogInfo($"Plan {doNoTxPlanID} already exists in corse {course.Id}. Ignoring"); }
        }
        /// <summary>
        /// private method used to propogate the isocenter IDs for CBCT imaging fields
        /// </summary>
        private async void PropagateIsocentersForCBCTselection()
        {
            Logger.LogInfo("Method called to propagate isocenter names for CBCT selection");
                try
                    {
                await esapiWorker.AsyncRunStructureContext((_patient, _structureSet) =>
                {
                    if (_collectionOfPriorityMatchIso != null && _collectionOfPriorityMatchIso.Count > 0)
                    {
                        Logger.LogInfo("Collection of isocenters is not empty. Cleaning it up.");
                        userInterface.Invoke(() =>
                        {
                            _collectionOfPriorityMatchIso.Clear();
                        });
                    }

                    //Define isocenter groups and save them into a list
                    Logger.LogInfo("Defining isocenter groups and saving them into a list");
                    VVector userOrigin = new VVector();
                    string isoKey(Beam b)
                    {
                        var v = b.IsocenterPosition;
                        Logger.LogInfo($"Isocenter captured: {Math.Round(v.x - userOrigin.x)}|{Math.Round(v.y - userOrigin.y)}|{Math.Round(v.z - userOrigin.z)} for beam {b.Id}");
                        return $"{Math.Round(v.x)}|{Math.Round(v.y)}|{Math.Round(v.z)}|";
                    }

                    ExternalPlanSetup planRO2C = null;
                    ExternalPlanSetup planFF = null;
                    StructureSet structureSet = null;

                    _patientId = _patient.Id;
                    foreach (var course in _patient.Courses)
                    {
                        foreach (var planSetup in course.ExternalPlanSetups)
                        {
                            if (_selectedRO2CCoursePlanID != null && _selectedRO2CCoursePlanID.Length > 0) 
                            {
                                var ro2cID = _selectedRO2CCoursePlanID.Split('/')[1];
                                if (planSetup.Id == ro2cID) planRO2C = planSetup;
                            }
                                
                            if (_selectedFFCoursePlanID != null && _selectedFFCoursePlanID.Length > 0)
                            {
                                var ffID = _selectedFFCoursePlanID.Split('/')[1];
                                if (planSetup.Id == ffID) planFF = planSetup;
                            }  
                        }
                    }
                    List<IGrouping<string, Beam>> groupsRO2C = new List<IGrouping<string, Beam>>();
                    List<IGrouping<string, Beam>> groupsFF = new List<IGrouping<string, Beam>>();
                    List<IGrouping<string, Beam>> groupsAll = new List<IGrouping<string, Beam>>();

                    if (planRO2C != null)
                    {
                        structureSet = planRO2C.StructureSet;
                        userOrigin = structureSet.Image.UserOrigin;
                        groupsRO2C = planRO2C.Beams.
                        Where(b => !b.IsSetupField)
                        .GroupBy(isoKey).
                        ToList();
                    }
                    if (planFF != null)
                    {
                        structureSet = planFF.StructureSet;
                        userOrigin = structureSet.Image.UserOrigin;
                        groupsFF = planFF.Beams.
                        Where(b => !b.IsSetupField)
                        .GroupBy(isoKey).
                        ToList();
                    }
                    if (groupsRO2C.Count > 0) groupsAll.AddRange(groupsRO2C);
                    if (groupsFF.Count > 0) 
                    { 
                        foreach(var group in groupsFF)
                        {
                            if (!groupsAll.Any(g => g.Key == group.Key))
                            {
                                groupsAll.Add(group);
                            }
                        }    
                    }

                    int groupN = 1;
                    foreach (var group in groupsAll)
                    {
                        string fieldID = new string(group.FirstOrDefault().Id.ToString().ToArray());
                        fieldID = SelectPlanOrRpIDBasedOnInputString(fieldID);
                        userInterface.Invoke(() =>
                        {
                            IsoItemModel test = new IsoItemModel(groupN, fieldID);
                            _collectionOfPriorityMatchIso.Add(test);
                        });
                        groupN++;
                    }
                });
            }
            catch (Exception exception)
            {
                //Log any appeared issues
                Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                System.Windows.MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
            }
        }
        private static List<BeamToPlanID_Model> LoadRegionRules(string _regionRulesXmlPath)
        {
            if (!File.Exists(_regionRulesXmlPath))
            {
                Logger.LogError($"Region rules XML not found: {_regionRulesXmlPath}");
                throw new FileNotFoundException($"Region rules XML not found: {_regionRulesXmlPath}");
            }

            var document = XDocument.Load(_regionRulesXmlPath);

            var rules = document
                .Root
                .Elements("RegionRule")
                .Select(x => new BeamToPlanID_Model
                {
                    Pattern = (string)x.Attribute("Pattern"),
                    Label = (string)x.Attribute("Label")
                })
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Pattern) &&
                    !string.IsNullOrWhiteSpace(x.Label))
                .ToList();

            Logger.LogInfo($"Loaded {rules.Count} region rules from XML.");

            foreach (var rule in rules)
            {
                Logger.LogInfo($"Rule loaded: Pattern='{rule.Pattern}', Label='{rule.Label}'");
            }

            return rules;

        }
        #endregion
    }
}
