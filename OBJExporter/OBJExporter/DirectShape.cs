using Autodesk.DesignScript.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.Diagnostics;
using RevitServices;
using Revit.GeometryConversion;
using Utils;

namespace GeometryTranslationExperiments
{
    public class DirectShape
    {
        public static int CreateDirectShape(Geometry geo, int graphicsStyle, string name)
        {
            List<Autodesk.DesignScript.Geometry.Point> points;
            List<IndexGroup> indexGroups;
            MeshUtils.TessellateGeoToMesh(geo, out points, out indexGroups);
            return NewDirectShape(points, indexGroups, RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
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
                var indicies = new List<uint>() { f.A, f.B, f.C, f.D };
                for (int i = 0; i < f.Count; i++)
                {
                    var currentindex = Convert.ToInt32(indicies[i]);
                    Debug.Assert(vertices.Count > currentindex,
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
