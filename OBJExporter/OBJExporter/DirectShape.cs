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

        public static void CreateDirectShapeBarFromPointComponentLists(List <double> sx ,List <double> sy ,List<double> sz ,List<double> ex ,List<double> ey ,List<double> ez )
        {

            var startPoints = new List<Autodesk.DesignScript.Geometry.Point>();
            var endPoints = new List<Autodesk.DesignScript.Geometry.Point>();
            for (int i = 0; i < sx.Count(); i++)
            {
                startPoints.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(sx[i],sy[i],sz[i]) );
                endPoints.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(ex[i],ey[i],ez[i]) );
            
            }

            var meshes = new List<Autodesk.DesignScript.Geometry.Mesh>();

           for(int i = 0; i< startPoints.Count;i++)
            {
                meshes.Add(CreateMeshBarFromPoints(startPoints[i], endPoints[i],0.05,3));
            }

            //dispose all other geo

           foreach (IDisposable startpoint in startPoints)
           {
               startpoint.Dispose();
           }

           foreach (IDisposable endpoint in endPoints)
           {
               endpoint.Dispose();
           }

            //now create direction shapes
           var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
           RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
            foreach (var mesh in meshes)
           {
               CreateDirectShapeByMesh(mesh, 1, "astrut");
               mesh.Dispose();
           }
            RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();

          

         
        }


        internal static Autodesk.DesignScript.Geometry.Mesh CreateMeshBarFromPoints( Autodesk.DesignScript.Geometry.Point startPoint, Autodesk.DesignScript.Geometry.Point endPoint, double radius = 8, int edgeCount = 6)
        {
            Autodesk.DesignScript.Geometry.Line distance = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(startPoint, endPoint);
            Vector tangentVector = distance.Direction;
            Circle startCircle = Circle.ByCenterPointRadiusNormal(startPoint, radius, tangentVector);
            Circle endCircle = Circle.ByCenterPointRadiusNormal(endPoint, radius, tangentVector);

            List<Autodesk.DesignScript.Geometry.Point> vertexList = new List<Autodesk.DesignScript.Geometry.Point>();
            List<IndexGroup> indiceList = new List<IndexGroup>();

            for (int i = 0; i < edgeCount; i++)//start circle
            {
                vertexList.Add(startCircle.PointAtParameter(i / (double)edgeCount));//not casting to double will fail the division silently returning 0

            }
            for (int i = 0; i < edgeCount; i++)//end circle
            {
                vertexList.Add(endCircle.PointAtParameter(i / (double) edgeCount));
            }

            for (int i = 0; i < edgeCount; i++)//facets
            {
                if (i < edgeCount - 1)
                {
                    indiceList.Add(IndexGroup.ByIndices((uint)i, (uint)i + 1, (uint)edgeCount + (uint)i + 1));//clockwise orientation of vertices [startCircle[0], startCircle[1], endCircle[0],
                    indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)i + 1, (uint)edgeCount + (uint)i, //clockwise orientation of vertices [endCircle[1], endCircle[0], startCircle[0],
                        (uint) i));
                }
                else if (i == edgeCount - 1) //stitch last strip to first
                {
                    indiceList.Add(IndexGroup.ByIndices((uint)i, (uint)0, (uint)edgeCount + (uint)0));
                    indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)0, (uint)edgeCount + (uint)i,
                        (uint)i));
                }
            }
            

            Autodesk.DesignScript.Geometry.Mesh extrudedBar = Autodesk.DesignScript.Geometry.Mesh.ByPointsFaceIndices(vertexList,indiceList);


            //cleanup local vars
            distance.Dispose();
            tangentVector.Dispose();
            startCircle.Dispose();
            endCircle.Dispose();
            vertexList.Clear();
            indiceList.Clear();

            foreach (IDisposable item in vertexList)
            {
                item.Dispose();
            }
            foreach (IDisposable item in indiceList)
            {
                item.Dispose();
            }

            return extrudedBar;
        }



        public static int CreateDirectShape(Geometry geo, int graphicsStyle, string name)
        {
            List<Autodesk.DesignScript.Geometry.Point> points;
            List<IndexGroup> indexGroups;
            MeshUtils.TessellateGeoToMesh(geo, out points, out indexGroups);
            return NewDirectShape(points, indexGroups, RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
        }

        public static int CreateDirectShapeByMesh(Autodesk.DesignScript.Geometry.Mesh mesh, int graphicsStyle, string name)
        {
          
            return NewDirectShape(mesh.VertexPositions.ToList(),mesh.FaceIndices.ToList(), RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
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

          

            var ds = Autodesk.Revit.DB.DirectShape.CreateElement(
              doc, _categoryId, appGuid, shapeName);

            ds.SetShape(r.GetGeometricalObjects());
            ds.Name = shapeName;
           
            Debug.Print(
              "Shape '{0}': added {1} faces, faces{2} failed.",
              shapeName, nFaces,
              nFacesFailed);

            return nFaces;
        }
    }
}
