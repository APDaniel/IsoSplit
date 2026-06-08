using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace IsoSplitProject.Helpers
{
    public class ESAPIvectorsHelper
    {
        private IDictionary<int, IEnumerable<IEnumerable<VVector>>> Contours { get; set; }

        private IDictionary<int, IEnumerable<IEnumerable<VVector>>> OriginalContours;
        private Structure structureToMove { get; set; }
        private StructureSet structureSet { get; set; }
        private VVector displacement { get; set; }
        public ESAPIvectorsHelper(VVector _displacement, Structure _structureToMove, StructureSet _structureSet) 
        {
            structureToMove = _structureToMove;
            structureSet = _structureSet;
            displacement = _displacement;
            OriginalContours = GetContours();
            Contours = OriginalContours;
        }

        public Dictionary<int, IEnumerable<IEnumerable<VVector>>> GetContours() 
        {
            var mesh = MeshBoundSlices();
            return Enumerable.Range(mesh.Key, mesh.Value).Where(x => structureToMove.GetContoursOnImagePlane(x).Any()).
                ToDictionary(y => y, y => structureToMove.GetContoursOnImagePlane(y).Select(x => x.Select(z => new VVector(z.x, z.y, z.z))));
        }
        public KeyValuePair<int,int> MeshBoundSlices() 
        {
            var mesh = structureToMove.MeshGeometry.Bounds;
            var meshLow = GetSlice(mesh.Z);
            var meshUp = GetSlice(mesh.Z + mesh.SizeZ) + 1;
            return new KeyValuePair<int, int>(meshLow, meshUp);
        }
        public int GetSlice(double z) 
        {
            var imageResolution = structureSet.Image.ZRes;
            return Convert.ToInt32((z - structureSet.Image.Origin.z) / imageResolution);
        }
        public VVector[][] MoveContour(VVector[][] contoursOnPlane) 
        {
            var copy = (VVector[][])contoursOnPlane.Clone();
            for (int i=0; i < contoursOnPlane.Length; i++) 
            {
                var contour = contoursOnPlane[i];
                var copy_c = copy[i];
                for(int j = 0; j < contour.Length; j++) 
                {
                    copy_c[j] = contour[j] + displacement;
                }
            }
            return copy;
        }
        public int PlaneToContour(int actualPlane) 
        {
            var desiredSliceDisplacement = displacement.z;
            var plane = (int)Math.Round((desiredSliceDisplacement/structureSet.Image.ZRes),1);
            return actualPlane + plane; 
        }
        public SegmentVolume MoveStructure() 
        {
            //Create a placeholder structure
            Logger.LogInfo("...Creating a placeholder structure");
            Structure placeholder = structureSet.AddStructure("CONTROL", StructureUniqueIdHelper.CheckIfTheStructureIDisUnique("PDtoDel", structureSet));
            Logger.LogInfo(string.Format($"...The placeholder structure: '{placeholder.Id}' has been created"));

            //Call a method to define volume for the placeholder and make a high resolution check
            if (structureToMove.IsHighResolution == true)
            {
                placeholder.ConvertToHighResolution();
                Logger.LogInfo(string.Format($"...Converting: '{placeholder.Id}' into high resolution"));
            }

            //Loop through contours and move each of them on the pre-defined value of displacement
            Logger.LogInfo("...Entering the loop to move contours for the placeholder");
            foreach (var contour in Contours)
            {
                var contourOnImagePlane = contour.Value;
                var movedContour = MoveContour(contourOnImagePlane.Select(x => x.ToArray()).ToArray());
                foreach (var vector in movedContour)
                {
                    placeholder.AddContourOnImagePlane(vector, PlaneToContour(contour.Key));
                }
            }

            //Create a segment volume to return
            var newSegmentVolume = placeholder.SegmentVolume;

            //Remove placeholder
            structureSet.RemoveStructure(placeholder);

            return newSegmentVolume;
        }
    }
}
