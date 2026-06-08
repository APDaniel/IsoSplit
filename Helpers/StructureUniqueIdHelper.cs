using System;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace IsoSplitProject.Helpers
{
    public class StructureUniqueIdHelper
    {
        public static string CheckIfTheStructureIDisUnique(string structureToSaveID, StructureSet structureSetToCheck) 
        {
            Logger.LogInfo(string.Format("Called a method to check if the structure ID: {0} is unique and does not exceed 16 characters", structureToSaveID));
            if (structureToSaveID.Length >= 16)
            {
                //delete '_' if length is >16
                if (structureToSaveID.Length >= 16)
                {
                    Logger.LogInfo("...Structure ID is too long. Trying to resolve the issue getting rid of '_'");
                    string toDelete = "_";
                    string updatedStructureToSaveID1 = "";
                    for (int i = 0; i < structureToSaveID.Length; i++)
                    {
                        if (!toDelete.Contains(structureToSaveID[i]))
                        { updatedStructureToSaveID1 += structureToSaveID[i]; };
                    }
                    if (updatedStructureToSaveID1.Length > 1)
                    {
                        structureToSaveID = updatedStructureToSaveID1;
                    }
                }

                //delete vowels if length is >16
                if (structureToSaveID.Length >= 16)
                {
                    Logger.LogInfo("...Structure ID is too long. Trying to resolve the issue getting rid of vowels");
                    //delete vowels
                    string vowels = "AEIOUaeiou";
                    string updatedStructureToSaveID = "";
                    for (int i = 0; i < structureToSaveID.Length; i++)
                    {
                        if (!vowels.Contains(structureToSaveID[i]))
                        { updatedStructureToSaveID += structureToSaveID[i]; };
                    }
                    if (updatedStructureToSaveID.Length > 1)
                    {
                        structureToSaveID = updatedStructureToSaveID;
                    }
                }


                //delete numbers if length is still>16
                if (structureToSaveID.Length >= 16)
                {
                    Logger.LogInfo("...Structure ID is too long. Trying to resolve the issue getting rid of numbers");
                    string toDelete = "1234567890";
                    string updatedStructureToSaveID1 = "";
                    for (int i = 0; i < structureToSaveID.Length; i++)
                    {
                        if (!toDelete.Contains(structureToSaveID[i]))
                        { updatedStructureToSaveID1 += structureToSaveID[i]; };
                    }
                    if (updatedStructureToSaveID1.Length > 1)
                    {
                        structureToSaveID = updatedStructureToSaveID1;
                    }
                }

                //delete excessive letters if length is still>16
                if (structureToSaveID.Length >= 16)
                {
                    Logger.LogInfo("...Structure ID is too long. Trying to resolve the issue getting rid of excessive letters");
                    int numberOfLettersToDelete = structureToSaveID.Length - 16;
                    int numberOfLettersToKeep = structureToSaveID.Length - numberOfLettersToDelete;
                    structureToSaveID = structureToSaveID.Substring(0, numberOfLettersToKeep);
                }

                Logger.LogInfo(string.Format("...Corrected structure ID is: {0}", structureToSaveID));
            }
            Logger.LogInfo("...Checking if the structure ID is unique");
            //Check if the structure name is unique
            var _checkIfStructureIdUnique = structureSetToCheck.Structures.FirstOrDefault(x => x.Id.ToLower() == structureToSaveID.ToLower());

            if (_checkIfStructureIdUnique != null)
            {
                structureToSaveID = structureToSaveID + "_";
                Logger.LogInfo("...The structure ID is not unique, correcting");
                for (int i = 1; i < 99; i++)
                {
                    if (0 < i && i < 10) 
                    {
                        if (char.IsDigit(structureToSaveID[structureToSaveID.Length - 2])) structureToSaveID=structureToSaveID.Remove(structureToSaveID.Length - 2);
                        if (char.IsDigit(structureToSaveID[structureToSaveID.Length - 1])) structureToSaveID=structureToSaveID.Remove(structureToSaveID.Length - 1);
                        structureToSaveID = structureToSaveID + i.ToString();
                    }
                    
                    if (i > 9 && structureToSaveID.Length < 2) structureToSaveID = structureToSaveID.Substring(0, structureToSaveID.Length) + i.ToString();
                    if (i > 9 && structureToSaveID.Length > 1)
                    {
                        if (char.IsDigit(structureToSaveID[structureToSaveID.Length - 2])) structureToSaveID=structureToSaveID.Remove(structureToSaveID.Length - 2);
                        if (char.IsDigit(structureToSaveID[structureToSaveID.Length - 1])) structureToSaveID=structureToSaveID.Remove(structureToSaveID.Length - 1);
                        structureToSaveID = structureToSaveID + i.ToString();
                    }
                    var check = structureSetToCheck.Structures.FirstOrDefault(x => x.Id.ToLower() == structureToSaveID.ToLower());
                    if (check == null) break;
                }
                Logger.LogInfo(string.Format("...Corrected structure ID is: {0}", structureToSaveID));
            }
            return structureToSaveID;
        }
    }
}
