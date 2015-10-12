using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.DB;
using Revit.GeometryConversion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryTranslationExperiments
{
    public static class DirectShapeInstance
    {
      /// <summary>
      /// a node to create instances from types identifie by hashing properties of each geometrical type, like length or diameter
      /// </summary>
      /// <param name="filepath">the file path to import a csv from, this csv should contain point pairs on each row, and also any other data to define the type... </param>
      /// <param name="lengthTol"></param>
      /// <param name="diameterTol"></param>
      /// <param name="baseGeo"></param>
      /// <returns></returns>
        public static Dictionary<string, Tuple<GeometryObject, int>> CreateInstancesFromHashedTypes(string filepath,int lengthTol, int diameterTol,Geometry baseGeo)
        {
            //create a new library, both a return object, and the real lib
            var returnlibrary = new Dictionary<string, Tuple<GeometryObject, int>>();
            var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            var lib = DirectShapeLibrary.GetDirectShapeLibrary(doc);
            lib.Reset();

            ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
            
            using (StreamReader r = new StreamReader(filepath))
            {
                //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...

                while (!r.EndOfStream)
                {

                    var line = r.ReadLine();
                    var cells = line.Split(',');
                    var startPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[0]), double.Parse(cells[1]), double.Parse(cells[2]));
                    var endPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[3]), double.Parse(cells[4]), double.Parse(cells[5]));

                    //create a line from start to end
                    var geoline = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(startPoint, endPoint);
                    var key = Math.Round(geoline.Length, lengthTol).ToString();
                    var dubkey = Math.Round(geoline.Length, lengthTol);

                    //if the library doesnt have this key then generate some new geometry
                    if (!returnlibrary.ContainsKey(key))
                    {
                        //scale the cube so that it, the same length as the line
                        var scaledcube = baseGeo.Scale(1.0, Math.Max(dubkey, 0.01), 1.0) as Autodesk.DesignScript.Geometry.Solid;
                        var revgeo = scaledcube.ToRevitType();
                        lib.AddDefinition(key, revgeo.First());
                        returnlibrary.Add(key, Tuple.Create(revgeo.First(), 0));
                        scaledcube.Dispose();
                    }

                    //in either case add the transform for this line

                    //get a cs in the center of the line
                    var cs = geoline.CoordinateSystemAtParameter(.5);

                    //so now rotate the cube so that it matches the lines rotation...
                    var revtransform = cs.ToTransform();
                    if (!revtransform.IsConformal)
                    {
                        throw new Exception("should have been conformal");
                    }

                    //now store the new count in the retur dict
                    var oldval = returnlibrary[key];
                    returnlibrary[key] = Tuple.Create(oldval.Item1,oldval.Item2+1);

                    //actually instantiate the geometry
                    var inst = Autodesk.Revit.DB.DirectShape.CreateGeometryInstance(doc, key, revtransform);

                    RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
                    var shape = Autodesk.Revit.DB.DirectShape.CreateElement(doc, categoryId, new Guid().ToString(), new Guid().ToString());
                    shape.SetShape(inst);
                    RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();

                    cs.Dispose();
                    geoline.Dispose();

                }


            }

            return returnlibrary;
        }


        /// <summary>
        /// this is a test method that creates an instance library from geometry to transforms - each piece of geometry is hashed to use as
        /// a key using some geometrical properties - like the length as in this test.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static Dictionary<string, Tuple<GeometryObject, List<Transform>>> buildInstanceLibraryFromFilePath(string filepath)
        {
            var library = new Dictionary<string, Tuple<GeometryObject, List<Transform>>>();
            var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            var basecube = Cuboid.ByLengths(.5, .5, 1);
            ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);

            using (StreamReader r = new StreamReader(filepath))
            {
                //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...

                while (!r.EndOfStream)
                {
                    
                    var line = r.ReadLine();
                    var cells = line.Split(',');
                    var startPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[0]), double.Parse(cells[1]), double.Parse(cells[2]));
                    var endPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[3]), double.Parse(cells[4]), double.Parse(cells[5]));

                    //create a line from start to end
                    var geoline = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(startPoint, endPoint);
                    var key = Math.Round(geoline.Length, 2).ToString();

                    //if the library doesnt have this key then generate some new geometry
                    if (!library.ContainsKey(key))
                    {
                        //scale the cube so that it, the same length as the line
                        var scaledcube = basecube.Scale(1, 1, geoline.Length) as Autodesk.DesignScript.Geometry.Solid;
                        var revgeo = ProtoToRevitMesh.ToRevitType(scaledcube);
                        library.Add(key, Tuple.Create(revgeo.First(), new List<Transform>()));
                        scaledcube.Dispose();
                    }

                    //in either case add the transform for this line

                    //get a point in the center of the line
                    var ori = geoline.PointAtParameter(.5);
                    var cs = geoline.CoordinateSystemAtParameter(0);

                    //so now rotate the cube so that it matches the lines rotation...
                    var transform = CoordinateSystem.ByOriginVectors(ori, cs.XAxis, cs.YAxis);
                    var revtransform = transform.ToTransform();

                    //library[key].Item2.Add(revtransform);

                  
                    cs.Dispose();
                    geoline.Dispose();
                    transform.Dispose();
                    ori.Dispose();

                }


            }

            return library;
        }
        /// <summary>
        /// this method is a test for using the directshape library to instance some geometry (a cube) for every strut of the mesh
        /// </summary>
        /// <param name="filepath"></param>
        public static void CreateDSInstancesShapeFromfilePath(string filepath)
        {
            var doc = RevitServices.Persistence.DocumentManager.Instance.CurrentDBDocument;
            var sampleInstance = Revit.GeometryConversion.ProtoToRevitMesh.ToRevitType(Cuboid.ByLengths(.5, .5, .5)).First();
            ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
            var lib = DirectShapeLibrary.GetDirectShapeLibrary(doc);
            lib.AddDefinition("sampleinstance", sampleInstance);
            var dyn = new Guid();

            using (StreamReader r = new StreamReader(filepath))
            {
                //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...


                while (!r.EndOfStream)
                {
                    var line = r.ReadLine();
                    var cells = line.Split(',');
                    var sp = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[0]), double.Parse(cells[1]), double.Parse(cells[2]));
                    var ep = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[3]), double.Parse(cells[4]), double.Parse(cells[5]));

                    var geoline = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(sp, ep);
                    var point = geoline.PointAtParameter(.5);
                    geoline.Dispose();

                    var translation = Autodesk.Revit.DB.Transform.CreateTranslation(point.ToXyz());

                    //Autodesk.Revit.DB.DirectShape.CreateElementInstance(doc, new ElementId(0), categoryId, "sampleinstance", translation,dyn.ToString(), new Guid().ToString());
                    var inst = Autodesk.Revit.DB.DirectShape.CreateGeometryInstance(doc, "sampleinstance", translation);

                    RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
                    var shape = Autodesk.Revit.DB.DirectShape.CreateElement(doc, categoryId, dyn.ToString(), new Guid().ToString());
                    shape.SetShape(inst);
                    RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();
                }


            }
        }
    }
}
