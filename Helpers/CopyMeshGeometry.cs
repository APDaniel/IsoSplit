using System.Windows.Media.Media3D;

namespace IsoSplitProject.Helpers
{
    public class CopyMeshGeometry
    {
        public static MeshGeometry3D CopyMeshGeometryHelper(MeshGeometry3D sourceMesh)
        {
            MeshGeometry3D copiedMesh = new MeshGeometry3D();
            if (sourceMesh != null)
            {
                // Copy Positions
                foreach (var point in sourceMesh.Positions)
                {
                    copiedMesh.Positions.Add(new Point3D(point.X, point.Y, point.Z));
                }

                // Copy TriangleIndices
                foreach (var index in sourceMesh.TriangleIndices)
                {
                    copiedMesh.TriangleIndices.Add(index);
                }

                // Copy Normals (optional)
                if (sourceMesh.Normals.Count > 0)
                {
                    foreach (var normal in sourceMesh.Normals)
                    {
                        copiedMesh.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));
                    }
                }
            }
            return copiedMesh;
        }
    }
}
