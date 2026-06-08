using System;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace IsoSplitProject.Helpers
{
    public class StructureCopyHelper
    {
        /// <summary>
        /// This method returns the segment volume of a structure as a segment volume of another structure with a assymetric user-defined margin applied.
        /// If the root structure is in high resolution, the new structure will be converted in high resolution as well
        /// </summary>
        /// <param name="newStructure">structure to save</param>
        /// <param name="structureToCopy">structure to grab</param>
        /// <param name="structureMarginGeometry"></param>
        /// <param name="x1">x axis margin in mm</param>
        /// <param name="y1">y axis margin in mm</param>
        /// <param name="z1">z axis margin in mm</param>
        /// <param name="x2">x axis opposite margin in mm</param>
        /// <param name="y2">y axis opposite margin in mm</param>
        /// <param name="z2">z axis opposite margin in mm</param>
        /// <returns></returns>
        public static SegmentVolume CopyStructureWithMargin(
            Structure newStructure, 
            Structure structureToCopy, 
            StructureMarginGeometry 
            structureMarginGeometry,
            int x1, int y1, int z1, int x2, int y2, int z2) 
        {
                string directionOfTheMargin = "NotDefined";
                if (structureMarginGeometry == StructureMarginGeometry.Inner) directionOfTheMargin = "Inner";
                if (structureMarginGeometry == StructureMarginGeometry.Outer) directionOfTheMargin = "Outer";

                Logger.LogInfo(string.Format(
                    "Called a method to copy volume of the structure: '{0}' to the structure {1} with the margin:x1={2}mm y1={3}mm z1={4}mm x2={5}mm y2={6}mm z2={7}mm, direction of the margin is: {8}",
                    structureToCopy.Id, newStructure.Id, x1, y1, z1, x2, y2, z2, directionOfTheMargin));

                //Check if the desired structure is in high resolution. If so, convert the new structure into high resolution too
                if (structureToCopy.IsHighResolution == true)
                {
                    try { 
                    if (newStructure.CanConvertToHighResolution()==true) 
                        newStructure.ConvertToHighResolution(); 
                }
                    catch (Exception exception)
                    {
                        // Log any appeared issues
                        Logger.LogError(string.Format(
                        "Structure '{0}' cannot be converted to HighResolution. Probably, due to its DICOM tag? {1}",
                        newStructure.Id, newStructure.DicomType.ToString()));
                        Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                        MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                    }
                }
                Logger.LogInfo(string.Format(
                   "Creating the structure '{0}' volume={1}cc from the structure '{2}' volume={3}cc with the margin:x1={4}mm y1={5}mm z1={6}mm x2={7}mm y2={8}mm z2={9}mm, direction of the margin is: {10}",
                   newStructure.Id, Math.Round(newStructure.Volume,2),
                   structureToCopy.Id, Math.Round(structureToCopy.Volume, 2), 
                   x1, y1, z1, x2, y2, z2, directionOfTheMargin));

                newStructure.SegmentVolume = structureToCopy.AsymmetricMargin(
                    new AxisAlignedMargins(
                        structureMarginGeometry, x1, y1, z1, x2, y2, z2));

            return newStructure.SegmentVolume;
        }

        /// <summary>
        /// This method returns the segment volume of a structure as a segment volume of another structure with a assymetric user-defined margin applied.
        /// If the root structure is in high resolution, the new structure will be converted in high resolution as well
        /// </summary>
        /// <param name="newStructure">structure to save</param>
        /// <param name="structureToCopy">structure to grab</param>
        /// <param name="margin">uniform margin in mm. If you'd like to make an inner margin, just type a negative value</param>
        /// <returns></returns>
        public static SegmentVolume CopyStructureWithUniformMargin(
            Structure newStructure,
            Structure structureToCopy,
            int margin)
        {
            Logger.LogInfo(string.Format(
                "Called a method to copy volume of the structure: '{0}' to the structure {1} with the uniform margin: {2}mm",
                structureToCopy.Id, newStructure.Id, margin));

            //Check if the desired structure is in high resolution. If so, convert the new structure into high resolution too
            if (structureToCopy.IsHighResolution == true)
            {
                try {if(newStructure.CanConvertToHighResolution()==true) newStructure.ConvertToHighResolution(); }
                catch (Exception exception)
                {
                    // Log any appeared issues
                    Logger.LogError(string.Format(
                    "Structure '{0}' cannot be converted to HighResolution. Probably, due to its DICOM tag? {1}",
                    newStructure.Id, newStructure.DicomType.ToString()));
                    Logger.LogError(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                    MessageBox.Show(string.Format("{0}\r\n{1}\r\n{2}", exception.Message, exception.InnerException, exception.StackTrace));
                }
            }
            newStructure.SegmentVolume = structureToCopy.Margin(margin);
            Logger.LogInfo(string.Format(
                   "...Creating the structure '{0}' volume={1}cc from the structure '{2}' volume={3}cc with the margin of {4}mm",
                   newStructure.Id, Math.Round(newStructure.Volume, 2),
                   structureToCopy.Id, Math.Round(structureToCopy.Volume, 2),
                   margin));

            return newStructure.SegmentVolume;
        }
    }
}
