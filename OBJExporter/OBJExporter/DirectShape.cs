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
using Dynamo;
using System.IO;

namespace GeometryTranslationExperiments
{
    public class DirectShape
    {


        /// <summary>
        /// this method is a test for using the directshape library to instance some geometry for every strut of the mesh
        /// </summary>
        /// <param name="filepath"></param>
        public static void CreateDSInstancesShapeFromfilePath(string filepath)
        {
            var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            var sampleInstance = Revit.GeometryConversion.ProtoToRevitMesh.ToRevitType(Cuboid.ByLengths(.5,.5,.5)).First();
            ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
            var lib = DirectShapeLibrary.GetDirectShapeLibrary(doc);
            lib.AddDefinition("sampleinstance",sampleInstance);
            var dyn = new Guid();
           
            using (StreamReader r = new StreamReader(filepath))
            {
                //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...
               

                while (!r.EndOfStream)
                {
                        var line = r.ReadLine();
                        var cells = line.Split(',');
                        var sp = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[0]), double.Parse(cells[1]), double.Parse(cells[2]));
                        var ep =   Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[3]), double.Parse(cells[4]), double.Parse(cells[5]));

                    var geoline = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(sp,ep);
                    var point = geoline.PointAtParameter(.5);
                    geoline.Dispose();

                    var translation = Autodesk.Revit.DB.Transform.CreateTranslation(point.ToXyz());

                    //Autodesk.Revit.DB.DirectShape.CreateElementInstance(doc, new ElementId(0), categoryId, "sampleinstance", translation,dyn.ToString(), new Guid().ToString());
                    var inst =Autodesk.Revit.DB.DirectShape.CreateGeometryInstance(doc, "sampleinstance", translation);

                    RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
                    var shape = Autodesk.Revit.DB.DirectShape.CreateElement(doc, categoryId, dyn.ToString(), new Guid().ToString());
                    shape.SetShape(inst);
                    RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
                }

                
            }
        }



        public static List<Autodesk.DesignScript.Geometry.Mesh> CreateMeshesFromFilePath(string filepath,int strutsPerMesh,double radius,int edges)
        
        {

             var outerMeshes = new List<Autodesk.DesignScript.Geometry.Mesh>();
             using (StreamReader r = new StreamReader(filepath))
             {
                 //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...

                 while (!r.EndOfStream)
                 {
                     var sx = new List<double>();
                     var sy = new List<double>();
                     var sz = new List<double>();

                     var ex = new List<double>();
                     var ey = new List<double>();
                     var ez = new List<double>();

                     for (int i = 0; i < strutsPerMesh; i++)
                     {
                         var line = r.ReadLine();
                         if (line == null)
                         {
                             break;
                         }
                         var cells = line.Split(',');
                         sx.Add(double.Parse(cells[0]));
                         sy.Add(double.Parse(cells[1]));
                         sz.Add(double.Parse(cells[2]));

                         ex.Add(double.Parse(cells[3]));
                         ey.Add(double.Parse(cells[4]));
                         ez.Add(double.Parse(cells[5]));
                
                     }
                     var meshes = GenerateMeshesFromPointLists(sx, sy, sz, ex, ey, ez, strutsPerMesh, radius, edges);
                     outerMeshes.Add(meshes[0]);
                 }
             }

             return outerMeshes;
        }


        public static void CreateDirectShapeBarFromFilePath(string filepath, int strutsPerMesh, double radius, int edges)
        {

             var outerMeshes = new List<Autodesk.DesignScript.Geometry.Mesh>();
             using (StreamReader r = new StreamReader(filepath))
             {
                 //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...

                 while (!r.EndOfStream)
                 {
                     var sx = new List<double>();
                     var sy = new List<double>();
                     var sz = new List<double>();

                     var ex = new List<double>();
                     var ey = new List<double>();
                     var ez = new List<double>();

                     for (int i = 0; i < strutsPerMesh; i++)
                     {
                         var line = r.ReadLine();
                         var cells = line.Split(',');
                         sx.Add(double.Parse(cells[0]));
                         sy.Add(double.Parse(cells[1]));
                         sz.Add(double.Parse(cells[2]));

                         ex.Add(double.Parse(cells[3]));
                         ey.Add(double.Parse(cells[4]));
                         ez.Add(double.Parse(cells[5]));
                     }
                     var meshes = GenerateMeshesFromPointLists(sx, sy, sz, ex, ey, ez, strutsPerMesh, radius, edges);
                     outerMeshes.AddRange(meshes);

                 }
             }

           //now create direction shapes
           var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
           RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);

               foreach (var mesh in outerMeshes)
               {
                   CreateDirectShapeByMesh(mesh, 1, "astrut");
                   mesh.Dispose();
               }
              
           RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
          
        }

        public static void CreateDirectShapeBarFromPointComponentLists(List <double> sx ,List <double> sy ,List<double> sz ,List<double> ex ,List<double> ey ,List<double> ez, int meshesPerDSInstance,double radius, int edges )
        {
            var meshes = GenerateMeshesFromPointLists(sx, sy, sz, ex, ey, ez, meshesPerDSInstance, radius, edges);
           //now create direction shapes
           var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
          

               foreach (var mesh in meshes)
               {
                   RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
                   CreateDirectShapeByMesh(mesh, 1, "astrut");

                   RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
                   mesh.Dispose();
               }
         
          
        }

        public static List<Autodesk.DesignScript.Geometry.Mesh> GenerateMeshesFromPointLists(List<double> sx, List<double> sy, List<double> sz, List<double> ex, List<double> ey, List<double> ez, int meshesPerDSInstance, double radius, int edges)
        {
            var startPoints = new List<Autodesk.DesignScript.Geometry.Point>();
            var endPoints = new List<Autodesk.DesignScript.Geometry.Point>();
            var meshes = new List<Autodesk.DesignScript.Geometry.Mesh>();

            for (int i = 0; i < sx.Count(); i++)
            {
                startPoints.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(sx[i], sy[i], sz[i]));
                endPoints.Add(Autodesk.DesignScript.Geometry.Point.ByCoordinates(ex[i], ey[i], ez[i]));
            }


            var startSublist = MeshUtils.Partition<Autodesk.DesignScript.Geometry.Point>(startPoints, meshesPerDSInstance);
            var endSublist = MeshUtils.Partition<Autodesk.DesignScript.Geometry.Point>(endPoints, meshesPerDSInstance);

            //now create a mesh from our subset of lines
            for (int index = 0; index < startSublist.Count(); index++)
            {

                var currentMesh = CreateSingleMeshTrussFromPoints(startSublist.ToList()[index].ToList(), endSublist.ToList()[index].ToList(), radius, edges);
                meshes.Add(currentMesh);
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
            return meshes;
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

        internal static Autodesk.DesignScript.Geometry.Mesh CreateSingleMeshTrussFromPoints(List<Autodesk.DesignScript.Geometry.Point> startPoints, List<Autodesk.DesignScript.Geometry.Point> endPoints, double radius = 8, int edgeCount = 6)
        {
            List<Autodesk.DesignScript.Geometry.Point> vertexList = new List<Autodesk.DesignScript.Geometry.Point>();
            List<IndexGroup> indiceList = new List<IndexGroup>();

            //foreach line pair we need to connect a polygon of points
            for (var index = 0; index < startPoints.Count; index++)
            {
                var distance = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(startPoints[index], endPoints[index]);
                var tangentVector = distance.Direction;
                var startCircle = Circle.ByCenterPointRadiusNormal(startPoints[index], radius, tangentVector);
                var endCircle = Circle.ByCenterPointRadiusNormal(endPoints[index], radius, tangentVector);

                uint offest = Convert.ToUInt32(index * (edgeCount * 2));

                for (int i = 0; i < edgeCount; i++)//start circle
                {
                    vertexList.Add(startCircle.PointAtParameter(i / (double)edgeCount));//not casting to double will fail the division silently returning 0

                }
                for (int i = 0; i < edgeCount; i++)//end circle
                {
                    vertexList.Add(endCircle.PointAtParameter(i / (double)edgeCount));
                }

                for (int i = 0; i < edgeCount; i++)//facets
                {
                    if (i < edgeCount - 1)
                    {
                        indiceList.Add(IndexGroup.ByIndices((uint)i + offest, (uint)i + 1 + offest, (uint)edgeCount + (uint)i + 1 + offest));//clockwise orientation of vertices [startCircle[0], startCircle[1], endCircle[0],
                        indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)i + 1 + offest, (uint)edgeCount + (uint)i + offest, //clockwise orientation of vertices [endCircle[1], endCircle[0], startCircle[0],
                            (uint)i + offest));
                    }
                    else if (i == edgeCount - 1) //stitch last strip to first
                    {
                        indiceList.Add(IndexGroup.ByIndices((uint)i + offest, (uint)0 + offest, (uint)edgeCount + (uint)0 + offest));
                        indiceList.Add(IndexGroup.ByIndices((uint)edgeCount + (uint)0 + offest, (uint)edgeCount + (uint)i + offest,
                            (uint)i + offest));
                    }
                }

                distance.Dispose();
                tangentVector.Dispose();
                startCircle.Dispose();
                endCircle.Dispose();

            }

            var allbars = Autodesk.DesignScript.Geometry.Mesh.ByPointsFaceIndices(vertexList, indiceList);

            foreach (IDisposable item in vertexList)
            {
                item.Dispose();
            }

            return allbars;
        }

        public static int CreateDirectShape(Geometry geo, int graphicsStyle, string name)
        {
            List<Autodesk.DesignScript.Geometry.Point> points;
            List<IndexGroup> indexGroups;
            MeshUtils.TessellateGeoToMesh(geo, out points, out indexGroups);
            return NewDirectShape(points.ToArray(), indexGroups.ToArray(), RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
        }

        public static int CreateDirectShapeByMesh(Autodesk.DesignScript.Geometry.Mesh mesh, int graphicsStyle, string name)
        {
            
            return NewDirectShape(mesh.VertexPositions,mesh.FaceIndices, RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument, new ElementId(graphicsStyle), Guid.NewGuid().ToString(), name);
          
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
          Autodesk.DesignScript.Geometry.Point[] vertices,
          IndexGroup[] faces,
          Document doc,
          ElementId graphicsStyleId,
          string appGuid,
          string shapeName)
        {
            int nFaces = 0;
            int nFacesFailed = 0;

            TessellatedShapeBuilder builder
              = new TessellatedShapeBuilder();

            //builder.LogString = shapeName;

            var corners = new List<Autodesk.DesignScript.Geometry.Point>();

            builder.OpenConnectedFaceSet(false);

            foreach (IndexGroup f in faces)
            {
               // builder.LogInteger = nFaces;

                corners.Clear();
                var indicies = new List<uint>() { f.A, f.B, f.C, f.D };
                for (int i = 0; i < f.Count; i++)
                {
                    var currentindex = Convert.ToInt32(indicies[i]);
                    //Debug.Assert(vertices.Length > currentindex,
                     // "how can the face vertex index be larger "
                     // + "than the total number of vertices?");

                 //   if (currentindex >= vertices.Length)
                  //  {
                  //      return -1;
                  //  }
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
                TessellatedShapeBuilderTarget.Mesh,
                TessellatedShapeBuilderFallback.Salvage,
                graphicsStyleId);

          

            var ds = Autodesk.Revit.DB.DirectShape.CreateElement(
              doc, _categoryId, appGuid, shapeName);

            ds.SetShape(r.GetGeometricalObjects());
            ds.Name = shapeName;
           
            //Debug.Print(
              //"Shape '{0}': added {1} faces, faces{2} failed.",
              //shapeName, nFaces,
              //nFacesFailed);

            return nFaces;
        }
    }
}
