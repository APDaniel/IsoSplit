using System.Linq;
using VMS.TPS.Common.Model.API;

namespace IsoSplitProject.Helpers
{
    public class DuplicateStructureSetHelper
    {
        public static void DuplicateStructureSet(Patient _patient, StructureSet _structureSet, StructureSet _duplicatedStructureSet, string _duplicatedStructureSetId) 
        {
            Logger.LogInfo("...Duplicating structure set");
            //Check if the duplicated structureSet ID is unique
            Logger.LogInfo("...Checking if the ID of duplicated structure set is unique");
            for (int i = 1; i < 9; i++)
            {
                _duplicatedStructureSetId = _duplicatedStructureSetId.Substring(0, _duplicatedStructureSetId.Length - 1) + i.ToString();

                var check = _patient.StructureSets.FirstOrDefault(x => x.Id == _duplicatedStructureSetId);
                if (check == null) break;
            }
            Logger.LogInfo(string.Format("...Decided to use ID: {0}", _duplicatedStructureSetId));

            Logger.LogInfo(string.Format("...Enagbling modifications for the patient: {0}", _patient.Id));
            _duplicatedStructureSet = _structureSet.Image.CreateNewStructureSet(); //Create new structureSet
            _duplicatedStructureSet.Id = _duplicatedStructureSetId; //Assign ss.Id
            Logger.LogInfo(string.Format("...Created structureSet with ID: ", _duplicatedStructureSetId));

            foreach (Structure structure in _structureSet.Structures) //Duplicate structures from one ss to another
            {
                var _DICOMtype = structure.DicomType;
                var _structureId = structure.Id;

                if (_DICOMtype == null || _DICOMtype == "") _DICOMtype = "CONTROL"; //If DICOM tag is empty, then assign it to "CONTROL"

                Structure _duplicatedStructure = _duplicatedStructureSet.AddStructure(_DICOMtype, _structureId);
                Logger.LogInfo(string.Format("...Copied structureSet with ID: ", _structureId));

                _duplicatedStructure.SegmentVolume = structure.SegmentVolume;
                Logger.LogInfo(string.Format("...Copied structure volume with ID: ", _structureId));
            }
        }
        
    }
}
