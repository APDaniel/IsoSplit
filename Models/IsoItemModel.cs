namespace IsoSplit.Models
{
    public class IsoItemModel
    {
        public string isocenterID { get; set; }
        public bool isSelectedAddCBCT { get; set; }
        public int groupNumber { get; set; }
        public string isocenterGroupText { get; set; }
        public IsoItemModel(int groupNumber, string isocenterID)
        {
            this.groupNumber = groupNumber;
            this.isocenterID = isocenterID;
            this.isSelectedAddCBCT = false;
            this.isocenterGroupText = $"Isocenter Group #{groupNumber}:";
        }
    }
}
