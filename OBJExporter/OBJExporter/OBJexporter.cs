using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.DesignScript.Geometry;
using Dynamo;
using DSCore;
using Autodesk.Revit.DB;
using System.Diagnostics;
using RevitServices;
using Revit.GeometryConversion;

//makes use of code forked from: http://wiki.unity3d.com/index.php?title=ExportOBJ
//which is covered by the createive commons share alike license
namespace OBJExporter

{
   
    public class OBJ
    {
        public Autodesk.DesignScript.Geometry.Mesh Mesh;
        public string StringRepresentation;

        public static List<List<T>> Split<T>(List<T> source, int subListLength)
        {
            return source.
               Select((x, i) => new { Index = i, Value = x })
               .GroupBy(x => x.Index / subListLength)
               .Select(x => x.Select(v => v.Value).ToList())
               .ToList();
        }

        public static OBJ byGeoAndColor(Geometry geo, DSCore.Color color)
        {
            var rpfactory = new DefaultRenderPackageFactory();
            var package = rpfactory.CreateRenderPackage();
            var param = new Autodesk.DesignScript.Interfaces.TessellationParameters();

            geo.Tessellate(package, param);

            var points = Split(package.MeshVertices.ToList(), 3).Select(x => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x[0], x[1], x[2])).ToList();
            var indicies = package.MeshIndices.ToList().Select(x => Convert.ToUInt32(x)).ToList();
            var indexGroupsints = Split(indicies, 3);

            var indexGroups = new List<IndexGroup>();
            for (int i = 0; i < indexGroupsints.Count; i++)
            {

                var a = indexGroupsints[i][0] - (2 + (i * 6));
                var b = indexGroupsints[i][1] - (4 + (i * 6));
                var c = indexGroupsints[i][2] - (6 + (i * 6));
                var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
                indexGroups.Add(newIndex);
            }

            var mesh = Autodesk.DesignScript.Geometry.Mesh.ByPointsFaceIndices(points, indexGroups);

            var obj = new OBJ();
            obj.Mesh = mesh;
            obj.StringRepresentation = ObjExporter.GenerateOBJstring(mesh, color, "cool name");
            return obj;
        }

        public string getOBJrepresentation()
        {
            return StringRepresentation;
        }

        public Autodesk.DesignScript.Geometry.Mesh getOBJMesh()
        {
            return this.Mesh;
        }

    }

    internal class ObjExporterScript
    {
        private static int StartIndex = 0;

        public static void Start()
        {
            StartIndex = 0;
        }
        public static void End()
        {
            StartIndex = 0;
        }


        public static string MeshToString(Autodesk.DesignScript.Geometry.Mesh mesh, DSCore.Color[] colors)
        {

            int numVertices = 0;
            DSCore.Color[] mats = colors;
            StringBuilder sb = new StringBuilder();

            foreach (Autodesk.DesignScript.Geometry.Point vertexPos in mesh.VertexPositions)
            {
                numVertices++;
                sb.Append(string.Format("v {0} {1} {2}\n", vertexPos.X, vertexPos.Y, vertexPos.Z));
            }
            sb.Append("\n");
            foreach (Vector vertexNormal in mesh.VertexNormals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", vertexNormal.X, vertexNormal.Y, vertexNormal.Z));
            }
            sb.Append("\n");
            for (int index = 0; index < mesh.VertexPositions.Count(); index++)
            {
                var uv = Autodesk.DesignScript.Geometry.UV.ByCoordinates(mesh.VertexPositions[index].X, mesh.VertexPositions[index].Y);
                sb.Append(string.Format("vt {0} {1}\n", uv.U, uv.V));
            }
            for (int colorindex = 0; colorindex < mats.Count(); colorindex++)
            {
                sb.Append("\n");
                //TODO fix this with some kind of color naming....
                sb.Append("usemtl ").Append(mats[colorindex].Red).Append("\n");
                sb.Append("usemap ").Append(mats[colorindex].Red).Append("\n");


                foreach (var index in mesh.FaceIndices)
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        index.A + 1, index.B + 1, index.C + 1));
            }

            return sb.ToString();
        }

    }

    internal static class ObjExporter
    {

        internal static string GenerateOBJstring(Autodesk.DesignScript.Geometry.Mesh mesh, DSCore.Color color, string meshName)
        {

            ObjExporterScript.Start();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                                + "\n#" + System.DateTime.Now.ToLongDateString()
                                + "\n#" + System.DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");


            meshString.Append(ObjExporterScript.MeshToString(mesh, new DSCore.Color[] { color }));
            meshString.Append("g ").Append(meshName).Append("\n");
            ObjExporterScript.End();
            return meshString.ToString();
        }

        /*static string processTransform(Transform t)
        {
            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + t.name
                            + "\n#-------"
                            + "\n");


            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf)
            {
                meshString.Append(ObjExporterScript.MeshToString(mf, t));
            }

            for (int i = 0; i < t.childCount; i++)
            {
                meshString.Append(processTransform(t.GetChild(i), makeSubmeshes));
            }

            return meshString.ToString();
        }
        */
    }

    public class DirectShape
    {
        public static int CreateDirectShape(Geometry geo,  int graphicsStyle, string name)
        {
            var rpfactory = new DefaultRenderPackageFactory();
            var package = rpfactory.CreateRenderPackage();
            var param = new Autodesk.DesignScript.Interfaces.TessellationParameters();

            geo.Tessellate(package, param);

            var points = OBJ.Split(package.MeshVertices.ToList(), 3).Select(x => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x[0], x[1], x[2])).ToList();
            var indicies = package.MeshIndices.ToList().Select(x => Convert.ToUInt32(x)).ToList();
            var indexGroupsints = OBJ.Split(indicies, 3);

            var indexGroups = new List<IndexGroup>();
            for (int i = 0; i < indexGroupsints.Count; i++)
            {

                var a = indexGroupsints[i][0] - (2 + (i * 6));
                var b = indexGroupsints[i][1] - (4 + (i * 6));
                var c = indexGroupsints[i][2] - (6 + (i * 6));
                var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
                indexGroups.Add(newIndex);
            }
            return NewDirectShape(points, indexGroups,RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
        }

        static ElementId _categoryId = new ElementId(
     BuiltInCategory.OST_GenericModel);

        //This method forked from: //https://github.com/jeremytammik/DirectObjLoader
        /// <summary>
        /// Create a new DirectShape element from given
        /// list of faces and return the number of faces
        /// processed.
        /// Return -1 if a face vertex index exceeds the
        /// total number of available vertices, 
        /// representing a fatal error.
        /// </summary>
        static int NewDirectShape(
          List<Autodesk.DesignScript.Geometry.Point> vertices,
          List<IndexGroup> faces,
          Document doc,
          ElementId graphicsStyleId,
          string appGuid,
          string shapeName)
        {
            int nFaces = 0;
            int nFacesFailed = 0;

            TessellatedShapeBuilder builder
              = new TessellatedShapeBuilder();

            builder.LogString = shapeName;

            var corners = new List<Autodesk.DesignScript.Geometry.Point>();

            builder.OpenConnectedFaceSet(false);

            foreach (IndexGroup f in faces)
            {
                builder.LogInteger = nFaces;

                corners.Clear();
                var indicies = new List<uint>(){ f.A, f.B, f.C, f.D};
                for (int i = 0; i < f.Count;i++ )
                {
                    var currentindex = Convert.ToInt32( indicies[i]);
                    Debug.Assert(vertices.Count >currentindex,
                      "how can the face vertex index be larger "
                      + "than the total number of vertices?");

                    if (currentindex >= vertices.Count)
                    {
                        return -1;
                    }
                    corners.Add(vertices[currentindex]);
                }

                //convert all the points to Revit XYZ vectors
                var xyzs = corners.Select(x => x.ToXyz()).ToList();
               
                try
                {
                   
                    builder.AddFace(new TessellatedFace(xyzs,
                      ElementId.InvalidElementId));

                    ++nFaces;
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                {
                    // Remember something went wrong here.

                    ++nFacesFailed;

                    Debug.Print(
                      "Revit API argument exception {0}\r\n"
                      + "Failed to add face with {1} corners: {2}",
                      ex.Message, corners.Count,
                      string.Join(", ",
                        corners));
                }
            }
            builder.CloseConnectedFaceSet();

            // Refer to StlImport sample for more clever 
            // handling of target and fallback and the 
            // possible combinations.

            TessellatedShapeBuilderResult r
              = builder.Build(
                TessellatedShapeBuilderTarget.AnyGeometry,
                TessellatedShapeBuilderFallback.Mesh,
                graphicsStyleId);

            RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);

            var ds = Autodesk.Revit.DB.DirectShape.CreateElement(
              doc, _categoryId, appGuid, shapeName);

            ds.SetShape(r.GetGeometricalObjects());
            ds.Name = shapeName;
            RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
            Debug.Print(
              "Shape '{0}': added {1} faces, faces{2} failed.",
              shapeName, nFaces,
              nFacesFailed);

            return nFaces;
        }
    }

}
