using Autodesk.DesignScript.Geometry;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Revit.Elements;
using Revit.GeometryConversion;
using RevitServices.Persistence;
using RevitServices.Transactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryTranslationExperiments
{


    public class familyInstanceHelpers
    {


        #region Private helper methods

        internal static FamilyInstanceCreationData GetCreationData(Autodesk.Revit.DB.Curve curve, Autodesk.Revit.DB.XYZ upVector, Autodesk.Revit.DB.Level level, Autodesk.Revit.DB.Structure.StructuralType structuralType, Autodesk.Revit.DB.FamilySymbol symbol)
        {

            //calculate the desired rotation
            //we do this by finding the angle between the z axis
            //and vector between the start of the beam and the target point
            //both projected onto the start plane of the beam.
            var zAxis = new XYZ(0, 0, 1);
            var yAxis = new XYZ(0, 1, 0);

            //flatten the beam line onto the XZ plane
            //using the start's z coordinate
            var start = curve.GetEndPoint(0);
            var end = curve.GetEndPoint(1);
            var newEnd = new XYZ(end.X, end.Y, start.Z); //drop end point to plane

            //catch the case where the end is directly above
            //the start, creating a normal with zero length
            //in that case, use the Z axis
            XYZ planeNormal = newEnd.IsAlmostEqualTo(start) ? zAxis : (newEnd - start).Normalize();

            double gamma = upVector.AngleOnPlaneTo(zAxis.IsAlmostEqualTo(planeNormal) ? yAxis : zAxis, planeNormal);

            return new FamilyInstanceCreationData(curve, symbol, level, structuralType)
            {
                RotateAngle = gamma
            };

        }

        internal static FamilyInstanceCreationData GetCreationData(Autodesk.Revit.DB.Curve curve, Autodesk.Revit.DB.Level level, Autodesk.Revit.DB.Structure.StructuralType structuralType, Autodesk.Revit.DB.FamilySymbol symbol)
        {
            return new FamilyInstanceCreationData(curve, symbol, level, structuralType);
        }

        #endregion
    }

          
    public static class DirectShapeInstance
    {


        public static int StructuralFramingFromFilePath(string filepath,Revit.Elements.FamilyType type,int batchSize)
        {
           var document = DocumentManager.Instance.CurrentDBDocument;
            FilteredElementCollector collector = new FilteredElementCollector(document);
           ICollection<Autodesk.Revit.DB.Element> collection = collector.OfClass(typeof(Autodesk.Revit.DB.Level)).ToElements();
           var level = (collection.First() as Autodesk.Revit.DB.Level);
           var count = 0;

           var BatchframeDatas = new List<FamilyInstanceCreationData>();

              using (StreamReader r = new StreamReader(filepath))
            {
                //we are going to chunk our CSV file as well, so we will not load the entire csv into memory at once...
                TransactionManager.Instance.EnsureInTransaction(DocumentManager.Instance.CurrentDBDocument);
                while (!r.EndOfStream)
                {
                    BatchframeDatas.Clear();
                    foreach(var current in Enumerable.Range(0,batchSize)){
                        if (r.EndOfStream)
                        {
                            break;
                        }
                       
                    var line = r.ReadLine();
                    var cells = line.Split(',');
                    var startPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[0]), double.Parse(cells[1]), double.Parse(cells[2]));
                    var endPoint = Autodesk.DesignScript.Geometry.Point.ByCoordinates(double.Parse(cells[3]), double.Parse(cells[4]), double.Parse(cells[5]));

                    //create a line from start to end
                    var geoline = Autodesk.DesignScript.Geometry.Line.ByStartPointEndPoint(startPoint, endPoint);
                    var creationData = familyInstanceHelpers.GetCreationData(geoline.ToRevitType(), level, Autodesk.Revit.DB.Structure.StructuralType.Beam, type.InternalElement as FamilySymbol);
                    BatchframeDatas.Add(creationData);

                    
                    count = count + 1;
                    geoline.Dispose();
                    }
                   
                    if (BatchframeDatas.Count > 0)
                    {
                        var elementIds = DocumentManager.Instance.CurrentDBDocument.Create.NewFamilyInstances2(BatchframeDatas);
                        foreach (var elementid in elementIds )
                        {
                            var ele = DocumentManager.Instance.CurrentDBDocument.GetElement(elementid);
                            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(ele as Autodesk.Revit.DB.FamilyInstance,0);
                            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(ele as Autodesk.Revit.DB.FamilyInstance, 1);
                        }
                    }
                   
                }
                TransactionManager.Instance.TransactionTaskDone();
                return count;

            }

        }

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
        public static Dictionary<string, Tuple<Geometry, int>> buildInstanceLibraryFromFilePath(string filepath, int lengthTol, Geometry baseGeo)
        {
            //create a new library, both a return object, and the real lib
            var returnlibrary = new Dictionary<string, Tuple<Geometry, int>>();
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
                        var scaledcube = baseGeo.Scale(1.0, Math.Max(dubkey, 0.01), 1.0);
                        //var revgeo = scaledcube.ToRevitType();
                        //lib.AddDefinition(key, revgeo.First());
                        returnlibrary.Add(key, Tuple.Create(scaledcube, 0));
                        //scaledcube.Dispose();
                    }

                    //in either case add the transform for this line

                    //get a cs in the center of the line
                   // var cs = geoline.CoordinateSystemAtParameter(.5);

                    //so now rotate the cube so that it matches the lines rotation...
                   // var revtransform = cs.ToTransform();
                    //if (!revtransform.IsConformal)
                   /// {
                   //     throw new Exception("should have been conformal");
                   // }

                    //now store the new count in the retur dict
                    var oldval = returnlibrary[key];
                    returnlibrary[key] = Tuple.Create(oldval.Item1, oldval.Item2 + 1);

                    //actually instantiate the geometry
                    //var inst = Autodesk.Revit.DB.DirectShape.CreateGeometryInstance(doc, key, revtransform);

                   // RevitServices.Transactions.TransactionManager.Instance.EnsureInTransaction(doc);
                    //var shape = Autodesk.Revit.DB.DirectShape.CreateElement(doc, categoryId, new Guid().ToString(), new Guid().ToString());
                    //shape.SetShape(inst);
                    //RevitServices.Transactions.TransactionManager.Instance.TransactionTaskDone();

                   // cs.Dispose();
                    geoline.Dispose();

                }


            }

            return returnlibrary;
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
