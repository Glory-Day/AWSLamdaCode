using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using MIConvexHull;
using System.Windows.Media;
using Petzold.Media3D;
using System.CodeDom.Compiler;

namespace DelaunayWPF
{
    class OBJNormalVertex
    {
        public int index, count;
        public double[] NormalVertex;
        public string NormalVertexQuery;

        public OBJNormalVertex(int idx, int cnt, double[] VN, string query)
        {
            index = idx;
            count = cnt;
            NormalVertex = VN;
            NormalVertexQuery = query;
        }

        public void ArrangeNormalVertex()
        {
            for (int i = 0; i < 3; i++)
                NormalVertex[i] /= count;

            NormalVertexQuery = string.Format("vn {0:0.00000000} {1:0.00000000} {2:0.00000000}", NormalVertex[0], NormalVertex[1], NormalVertex[2]);
        }
    }

    /// <summary>
    /// This class represents 3D triangulation of random data.
    /// </summary>
    class RandomTriangulation : ModelVisual3D
    {   
        IEnumerable<Tetrahedron> tetrahedrons;

        /// <summary>
        /// The count of the tetrahedrons.
        /// </summary>
        public int Count { get { return tetrahedrons.Count(); } }

        /// <summary>
        /// Creates a triangulation of random data.
        /// For larger data sets it might be a good idea to separate the triangulation calculation
        /// from creating the visual so that the triangulation can be computed on a separate threat.
        /// </summary>
        /// <param name="count">Number of vertices to generate</param>
        /// <param name="radius">Radius of the vertices</param>
        /// <param name="uniform"></param>
        /// <returns>Triangulation</returns>
        public static RandomTriangulation Create(int count, double radius, bool uniform, string[] textValue)
        {
            Random rnd = new Random();
            List<Vertex> vertices = new List<Vertex>();

            if (!uniform)
            {
                // generate some random points
                //Func<double> nextRandom = () => 2 * radius * rnd.NextDouble() - radius;
                //vertices = Enumerable.Range(0, count)
                //    .Select(_ => new Vertex(nextRandom(), nextRandom(), nextRandom()))
                //    .ToList();
                for (int i = 0; i < textValue.Length; i += 3)
                    vertices.Add(new Vertex(Convert.ToDouble(textValue[i]), Convert.ToDouble(textValue[i + 1]), Convert.ToDouble(textValue[i + 2])));
            }
            else
            {
                vertices = new List<Vertex>();
                int d = Math.Max((int)Math.Ceiling(Math.Sqrt(count)) / 2, 3);
                double cs = 2 * radius / (d - 1);
                for (int i = 0; i < d; i++)
                {
                    for (int j = 0; j < d; j++)
                    {
                        for (int k = 0; k < d; k++)
                        {
                            vertices.Add(new Vertex(cs * i - cs * (d - 1) / 2, cs * j - cs * (d - 1) / 2, cs * k - cs * (d - 1) / 2));
                        }
                    }
                }
            }
            var tetrahedrons = Triangulation.CreateDelaunay<Vertex, Tetrahedron>(vertices).Cells;

            // create a model for each tetrahedron, pick a random color
            Model3DGroup model = new Model3DGroup();
            foreach (var t in tetrahedrons)
            {
                var color = Color.FromArgb((byte)255, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
                model.Children.Add(t.CreateModel(color, radius));
            }

            //var redMaterial = new MaterialGroup 
            //{ 
            //    Children = new MaterialCollection
            //    {
            //        new DiffuseMaterial(Brushes.Red),
            //        // give it some shine
            //        new SpecularMaterial(Brushes.LightYellow, 2.0) 
            //    } 
            //};

            //var greenMaterial = new MaterialGroup
            //{
            //    Children = new MaterialCollection
            //    {
            //        new DiffuseMaterial(Brushes.Green),
            //        // give it some shine
            //        new SpecularMaterial(Brushes.LightYellow, 2.0) 
            //    }
            //};

            //var blueMaterial = new MaterialGroup
            //{
            //    Children = new MaterialCollection
            //    {
            //        new DiffuseMaterial(Brushes.Blue),
            //        // give it some shine
            //        new SpecularMaterial(Brushes.LightYellow, 2.0) 
            //    }
            //};

            //CylinderMesh c = new CylinderMesh() { Length = 10, Radius = 0.5 };
            //model.Children.Add(new GeometryModel3D { Geometry = c.Geometry, Material = greenMaterial });
            //model.Children.Add(new GeometryModel3D { Geometry = c.Geometry, Material = redMaterial, Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)) });
            //model.Children.Add(new GeometryModel3D { Geometry = c.Geometry, Material = blueMaterial, Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)) });

            var triangulation = new RandomTriangulation();
            triangulation.tetrahedrons = tetrahedrons;

            int OBJIndex = 1 ,OBJTextureIndex = 1;
            Dictionary<string, OBJNormalVertex> OBJNormalVertexList = new Dictionary<string, OBJNormalVertex>();
            Dictionary<string, string> OBJTextureKeyList = new Dictionary<string, string>();
            Dictionary<string, int> OBJTextureList = new Dictionary<string, int>();

            foreach (var Element in vertices)
            {
                string textVertex = string.Format("v {0} {1} {2}", Element.Position[0], Element.Position[1], Element.Position[2]);
                string textTexture = string.Format("vt {0} {1}", (Element.Position[0] + 50) / 100, (Element.Position[1] + 50) / 100);

                OBJTextureKeyList.Add(textVertex, textTexture);
                OBJNormalVertexList.Add(textVertex, new OBJNormalVertex(OBJIndex++, 0, new double[] { 0, 0, 0 }, ""));

                if (OBJTextureList.ContainsKey(textTexture) == false)
                {
                    OBJTextureList.Add(textTexture, OBJTextureIndex++);
                }
            }

            foreach (var Element in tetrahedrons)
            {
                for (int i = 0; i < 3; i++)
                {
                    string textVertex = string.Format("v {0} {1} {2}", Element.Vertices[i].Position[0], Element.Vertices[i].Position[1], Element.Vertices[i].Position[2]);

                    OBJNormalVertexList[textVertex].count++;
                    OBJNormalVertexList[textVertex].NormalVertex[0] += Element.Normal[0];
                    OBJNormalVertexList[textVertex].NormalVertex[1] += Element.Normal[1];
                    OBJNormalVertexList[textVertex].NormalVertex[2] += Element.Normal[2];
                }
            }

            foreach (var Element in OBJNormalVertexList)
                Element.Value.ArrangeNormalVertex();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\DEVMF\Desktop\File\VS\MIConvexHull-master\MIConvexHull-master\Examples\7DelaunayWPF\output.txt"))
            {
                foreach (var Element in OBJNormalVertexList)
                    file.WriteLine(Element.Key);
                file.WriteLine();

                foreach(var Element in OBJNormalVertexList)
                    file.WriteLine(Element.Value.NormalVertexQuery);
                file.WriteLine();

                foreach (var Element in OBJTextureList)
                    file.WriteLine(Element.Key);
                file.WriteLine();

                foreach (var Element in tetrahedrons)
                {
                    string query1 = string.Format("v {0} {1} {2}", Element.Vertices[0].Position[0], Element.Vertices[0].Position[1], Element.Vertices[0].Position[2]);
                    string query2 = string.Format("v {0} {1} {2}", Element.Vertices[1].Position[0], Element.Vertices[1].Position[1], Element.Vertices[1].Position[2]);
                    string query3 = string.Format("v {0} {1} {2}", Element.Vertices[2].Position[0], Element.Vertices[2].Position[1], Element.Vertices[2].Position[2]);

                    string face1 = string.Format("{0}/{1}/{2}", OBJNormalVertexList[query1].index, OBJTextureList[OBJTextureKeyList[query1]], OBJNormalVertexList[query1].index);
                    string face2 = string.Format("{0}/{1}/{2}", OBJNormalVertexList[query2].index, OBJTextureList[OBJTextureKeyList[query2]], OBJNormalVertexList[query2].index);
                    string face3 = string.Format("{0}/{1}/{2}", OBJNormalVertexList[query3].index, OBJTextureList[OBJTextureKeyList[query3]], OBJNormalVertexList[query3].index);

                    string face = string.Format("f {0} {1} {2}", face1, face2, face3);

                    file.WriteLine(face);
                }
            }

            //foreach (var Element in OBJVertexAndVormalVertex)
            //    if (Element.Value.isNegative)
            //        OBJVertexTexture.Add(Element.Value.vertexTexture, VTindex++);

            // assign the Visual3DModel property of the ModelVisual3D class
            triangulation.Visual3DModel = model;

            return triangulation;
        }

        /// <summary>
        /// Begins the expand animation.
        /// </summary>
        public void Expand()
        {
            foreach (var t in tetrahedrons) t.Expand();
        }

        /// <summary>
        /// Begins the expand random animation.
        /// </summary>
        public void ExpandRandom()
        {
            foreach (var t in tetrahedrons) t.ExpandRandom();
        }

        /// <summary>
        /// Begins the collapse animation.
        /// </summary>
        public void Collapse()
        {
            foreach (var t in tetrahedrons) t.Collapse();
        }

        /// <summary>
        /// Begins the pulse animation.
        /// </summary>
        public void Pulse()
        {
            foreach (var t in tetrahedrons) t.Pulse();
        }
    }
}
