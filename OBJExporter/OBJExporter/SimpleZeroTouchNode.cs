using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Geometry;
using DSCore;
using Display;
using Dynamo;

namespace Caadfutures
{
    public static class SimpleZeroTouchNodes
    {


        /// <summary>
        /// This is an example node demonstrating how to use the Zero Touch import mechanism.
        /// It returns the input number multiplied by 2.
        /// </summary>
        /// <param name="inputNumber">Number that will get multiplied by 2</param>
        /// <returns name="outputNumber">The result of the input number multiplied by 2</param>
        /// <search>
        /// example, multiply, math
        /// </search>
        /// 
        public static double MultByTwo(double inputNumber)
        {
            return inputNumber * 2.0;
        }






        [MultiReturn(new[] { "item1", "item2" })]
        public static Dictionary<string, object> Return2Things()
        {
            var returnDict = new Dictionary<string, object>();
            returnDict.Add("item1", 10);
            returnDict.Add("itme2", 20);
            return returnDict;
        }






        public static double OptionalArguments(double number1 = 1, double number2 = 2, double number3 = 3)
        {
            return number1 + number2 + number3;
        }
    }


    public static class FancyZeroTouchNodes
    {




        public static List<object> ANodeThatLoops(object itemToRepeat, int timesToRepeat)
        {
            var outputList = new List<object>();
            int i = 0;
            while (i < timesToRepeat)
            {
                outputList.Add(itemToRepeat);
                i++;
            }
            return outputList;
        }







        public static List<Geometry> ANodeThatCreatesGeometry(Curve somecurve)
        {
            var outputList = new List<Geometry>();
            var start = somecurve.StartPoint;
            var end = somecurve.EndPoint;

            var outputlist = new List<Geometry>();

            for (double i = 0; i < somecurve.EndParameter(); i = i + .1)
            {
                outputlist.Add(Sphere.ByCenterPointRadius(somecurve.PointAtParameter(i)));
            }

            return outputlist;

        }
    }

    public static class RandomizeSurface
    {

        public static Surface Randomize(Surface surface)
        {
            var urange = Enumerable.Range(0, 11).Select(x => x / 10.0);
            var pointList = new List<List<Point>>();
            System.Random random = new System.Random();
            foreach (var u in urange)
            {
                var isoline = new List<Point>();
                foreach (var v in urange)
                {
                    var point = surface.PointAtParameter(u, v);
                    var normal = surface.NormalAtParameter(u, v);
                    var newpoint = point.Add(normal.Scale((random.NextDouble() / 10.0) * random.Next(-3, 3)));
                    isoline.Add(newpoint);
                }
                pointList.Add(isoline);
            }

            var finalcurves = new List<Curve>();
            foreach (var subpointList in pointList)
            {
                var curve = NurbsCurve.ByPoints(subpointList);
                finalcurves.Add(curve);
            }
            return Surface.ByLoft(finalcurves);


        }






        public static Surface RandomizeWithDispose(Surface surface)
        {
            var oldGeo = new List<Geometry>();
            var urange = Enumerable.Range(0, 7).Select(x => x / 6.0);
            var pointList = new List<List<Point>>();
            System.Random random = new System.Random();
            foreach (var u in urange)
            {
                var isoline = new List<Point>();
                foreach (var v in urange)
                {
                    var point = surface.PointAtParameter(u, v);
                    var normal = surface.NormalAtParameter(u, v);
                    var newpoint = point.Add(normal.Scale((random.NextDouble() / 10.0) * random.Next(-3, 3)));
                    isoline.Add(newpoint);
                }
                pointList.Add(isoline);
            }

            var finalcurves = new List<Curve>();
            foreach (var subpointList in pointList)
            {
                var curve = NurbsCurve.ByPoints(subpointList);
                finalcurves.Add(curve);
            }
            var finalsurface = Surface.ByLoft(finalcurves);

            oldGeo.AddRange(pointList.SelectMany(x => x));
            oldGeo.AddRange(finalcurves);
            foreach (IDisposable item in oldGeo)
            {
                item.Dispose();
            }

            return finalsurface;



        }






        public static Surface SimpleSurfaceAreaMaximizer(Surface surface, int iterations)
        {
            int i = 0;
            double max = surface.Area;
            var currentSurface = surface;
            while (i < iterations)
            {
                var randomSurface = RandomizeWithDispose(currentSurface);
                var area = randomSurface.Area;
                if (area > max)
                {
                    max = area;
                    currentSurface = randomSurface;
                }
                i++;
            }

            return currentSurface;
        }

    }

    public class OBJ
    {
        public Mesh Mesh;
        public string StringRepresentation;

        public static List<List<T>> Split<T>(List<T> source, int subListLength)
        {
            return source.
               Select((x, i) => new { Index = i, Value = x })
               .GroupBy(x => x.Index / subListLength)
               .Select(x => x.Select(v => v.Value).ToList())
               .ToList();
        }

        public static OBJ byGeoAndColor(Geometry geo, Color color)
        {
            var rpfactory = new DefaultRenderPackageFactory();
            var package = rpfactory.CreateRenderPackage();
            geo.Tessellate(package);
            
            var points = Split(package.MeshVertices.ToList(), 3).Select(x => Point.ByCoordinates(x[0], x[1], x[2])).ToList();
            var indicies= package.MeshIndices.ToList().Select(x=>Convert.ToUInt32(x)).ToList();
            var indexGroupsints = Split(indicies, 3);

            var indexGroups = new List<IndexGroup>();
            for (int i = 0; i < indexGroupsints.Count; i++)
            {

                var a = indexGroupsints[i][0] - (2 + (i * 6));
                var b = indexGroupsints[i][1] -  (4 + (i * 6));
                var c = indexGroupsints[i][2] - (6 + (i * 6));
                var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
                indexGroups.Add(newIndex);
            }

            var mesh = Mesh.ByPointsFaceIndices(points, indexGroups);
           
            var obj = new OBJ();
            obj.Mesh = mesh;
            obj.StringRepresentation = ObjExporter.GenerateOBJstring(mesh, color,"cool name");
            return obj;
        }

        public string getOBJrepresentation()
        {
            return StringRepresentation;
        }

        public Mesh getOBJMesh()
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


        public static string MeshToString(Mesh mesh, Color[] colors) 
	{	
		
		int numVertices = 0;
        Color[] mats = colors;
		StringBuilder sb = new StringBuilder();
 
		foreach(Point vertexPos in mesh.VertexPositions)
		{
			numVertices++;
            sb.Append(string.Format("v {0} {1} {2}\n", vertexPos.X, vertexPos.Y, vertexPos.Z));
		}
		sb.Append("\n");
		foreach(Vector vertexNormal in mesh.VertexNormals) 
		{
            sb.Append(string.Format("vn {0} {1} {2}\n", vertexNormal.X, vertexNormal.Y, vertexNormal.Z));
		}
		sb.Append("\n");
        for (int index = 0; index < mesh.VertexPositions.Count(); index++)
        {
            var uv = UV.ByCoordinates(mesh.VertexPositions[index].X, mesh.VertexPositions[index].Y);
            sb.Append(string.Format("vt {0} {1}\n", uv.U, uv.V));
        }
		for (int colorindex=0; colorindex < mats.Count(); colorindex ++) 
		{
			sb.Append("\n");
            //TODO fix this with some kind of color naming....
			sb.Append("usemtl ").Append(mats[colorindex].Red).Append("\n");
			sb.Append("usemap ").Append(mats[colorindex].Red).Append("\n");
 
			
			foreach(var index in mesh.FaceIndices)
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
					index.A+1, index.B+1, index.C+1));
			}

        return sb.ToString();
		}
        
    }

    internal static class ObjExporter
    {

        internal static string GenerateOBJstring(Mesh mesh, Color color, string meshName)
        {

            ObjExporterScript.Start();

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                                + "\n#" + System.DateTime.Now.ToLongDateString()
                                + "\n#" + System.DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");


            meshString.Append(ObjExporterScript.MeshToString(mesh, new Color[]{color}));
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




}
