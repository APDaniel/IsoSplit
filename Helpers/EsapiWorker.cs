using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using VMS.TPS.Common.Model.API;

namespace IsoSplitProject.Helpers
{
    /// <summary>
    /// A class that helps to avoid freezing of the script window during script execution via Eclipse
    /// </summary>
    public class EsapiWorker
    {
        #region private fields
        private readonly StructureSet _structureSet = null;
        private readonly PlanSetup _planSetup = null;
        private readonly Patient _patient = null;
        private readonly Dispatcher _dispatcher = null;
        #endregion

        #region methods
        //A method to be used to run structure context
        public EsapiWorker(Patient patient, StructureSet structureSet)
        {
            _patient = patient; 
            _structureSet = structureSet;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        //A method to be used to run plan context
        public EsapiWorker(Patient patient, PlanSetup planSetup)
        {
            _patient = patient; 
            _planSetup = planSetup;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public delegate void D(Patient patient, StructureSet structureSet);

        public async Task<bool> AsyncRunStructureContext(Action<Patient, StructureSet> action)
        {
            await _dispatcher.BeginInvoke(action, _patient, _structureSet);
            return true;
        }

        public async Task<bool> AsyncRunPlanContext(Action<Patient, PlanSetup> action)
        {
            await _dispatcher.BeginInvoke(action, _patient, _planSetup);
            return true;
        }
        #endregion
    }
}
