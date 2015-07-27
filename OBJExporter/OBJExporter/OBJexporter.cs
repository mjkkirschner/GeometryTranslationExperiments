using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Geometry;
using Dynamo;
using DSCore;
using Utils;
using Dynamo.DSEngine;

//makes use of code forked from: http://wiki.unity3d.com/index.php?title=ExportOBJ
//which is covered by the createive commons share alike license

namespace Utils
{
    public static class MeshUtils
    {
        //use this version for .82
        //public static void TessellateGeoToMesh(Geometry geo, out List<Autodesk.DesignScript.Geometry.Point> points, out List<IndexGroup> indexGroups)
        //{
        //    var rpfactory = new DefaultRenderPackageFactory();
        //    var package = rpfactory.CreateRenderPackage();
        //    var param = new Autodesk.DesignScript.Interfaces.TessellationParameters();

        //    geo.Tessellate(package, param);


        //    points = Split(package.MeshVertices.ToList(), 3).Select(x => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x[0], x[1], x[2])).ToList();
        //    var indicies = package.MeshIndices.ToList().Select(x => Convert.ToUInt32(x)).ToList();
        //    var indexGroupsints = Split(indicies, 3);

        //    indexGroups = new List<IndexGroup>();
        //    for (int i = 0; i < indexGroupsints.Count; i++)
        //    {

        //        var a = indexGroupsints[i][0] - (2 + (i * 6));
        //        var b = indexGroupsints[i][1] - (4 + (i * 6));
        //        var c = indexGroupsints[i][2] - (6 + (i * 6));
        //        var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
        //        indexGroups.Add(newIndex);
        //    }
        //}
        
        //use this version for .80
        /* public static void TessellateGeoToMesh(Geometry geo, out List<Autodesk.DesignScript.Geometry.Point> points, out List<IndexGroup> indexGroups)
         {
                       
             var package = new RenderPackage();
             geo.Tessellate(package);

             points = Split(package.TriangleVertices.ToList(), 3).Select(x => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x[0], x[1], x[2])).ToList();
             //var indicies = points.Select(x => Convert.ToUInt32(points.IndexOf(x))).ToList();

             var indicies = new List<uint>();
             var index = 2;
            foreach(var point in points)
            {
                indicies.Add(Convert.ToUInt32(index));
                index = index + 3;
            }
            
             var indexGroupsints = Split(indicies, 3);

             indexGroups = new List<IndexGroup>();
             for (int i = 0; i < indexGroupsints.Count; i++)
             {

                 var a = indexGroupsints[i][0] - (2 + (i * 6));
                 var b = indexGroupsints[i][1] - (4 + (i * 6));
                 var c = indexGroupsints[i][2] - (6 + (i * 6));
                 var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
                 indexGroups.Add(newIndex);
             }
         }
         */
        //use this version for .81
        public static void TessellateGeoToMesh(Geometry geo, out List<Autodesk.DesignScript.Geometry.Point> points, out List<IndexGroup> indexGroups)
        {

            var package = new DefaultRenderPackage();
            geo.Tessellate(package);

            points = Split(package.MeshVertices.ToList(), 3).Select(x => Autodesk.DesignScript.Geometry.Point.ByCoordinates(x[0], x[1], x[2])).ToList();
            var indicies = package.MeshIndices.ToList().Select(x => Convert.ToUInt32(x)).ToList();
            var indexGroupsints = Split(indicies, 3);

            indexGroups = new List<IndexGroup>();
            for (int i = 0; i < indexGroupsints.Count; i++)
            {

                var a = indexGroupsints[i][0] - (2 + (i * 6));
                var b = indexGroupsints[i][1] - (4 + (i * 6));
                var c = indexGroupsints[i][2] - (6 + (i * 6));
                var newIndex = IndexGroup.ByIndices(Convert.ToUInt32(a), Convert.ToUInt32(b), Convert.ToUInt32(c));
                indexGroups.Add(newIndex);
            }
        }
        



        public static List<List<T>> Split<T>(List<T> source, int subListLength)
        {
            return source.
               Select((x, i) => new { Index = i, Value = x })
               .GroupBy(x => x.Index / subListLength)
               .Select(x => x.Select(v => v.Value).ToList())
               .ToList();
        }

    }

}

namespace GeometryTranslationExperiments
{

    public class OBJ
    {
        public Autodesk.DesignScript.Geometry.Mesh Mesh;
        public string StringRepresentation;

        public static OBJ byGeoAndColor(Geometry geo, DSCore.Color color)
        {
            List<Autodesk.DesignScript.Geometry.Point> points;
            List<IndexGroup> indexGroups;
            MeshUtils.TessellateGeoToMesh(geo, out points, out indexGroups);

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

    internal static class ObjExporterScript
    {

        public static string MeshToString(Autodesk.DesignScript.Geometry.Mesh mesh, DSCore.Color[] colors)
        {

            int numVertices = 0;
            DSCore.Color[] mats = colors;
            StringBuilder sb = new StringBuilder();
            var positions = mesh.VertexPositions;

            foreach (Autodesk.DesignScript.Geometry.Point vertexPos in positions)
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
            for (int index = 0; index < positions.Count(); index++)
            {
             
                sb.Append(string.Format("vt {0} {1}\n", positions[index].X, positions[index].Y));
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

            StringBuilder meshString = new StringBuilder();

            meshString.Append("#" + meshName + ".obj"
                                + "\n#" + System.DateTime.Now.ToLongDateString()
                                + "\n#" + System.DateTime.Now.ToLongTimeString()
                                + "\n#-------"
                                + "\n\n");


            meshString.Append(ObjExporterScript.MeshToString(mesh, new DSCore.Color[] { color }));
            meshString.Append("g ").Append(meshName).Append("\n");

            return meshString.ToString();
        }

    }

}
