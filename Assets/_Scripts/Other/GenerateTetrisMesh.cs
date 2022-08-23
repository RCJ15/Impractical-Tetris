using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Pathfinding.Poly2Tri;
using UnityEditor;
using System.Diagnostics; // For StopWatch
using Debug = UnityEngine.Debug;
#endif

namespace Tetris.Shapes
{
    /// <summary>
    /// A script that will automatically generate a <see cref="Mesh"/> for using a the polygons of a 2D collider. <para/>
    /// NOTE: This script will exist in builds, but will have no functionality.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    [RequireComponent(typeof(MeshFilter))]
    public class GenerateTetrisMesh : MonoBehaviour
    {
        public const float DIST = 0.125f;
        public static readonly Vector3 INSET_OFFSET = new Vector3(0, 0, -0.125f);

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private Vector3[] vertices;
        [SerializeField] private int[] triIndices;
        [SerializeField] private Vector3[] normals;

#if UNITY_EDITOR
        [SerializeField] private Collider2D _col;
        private static readonly Stopwatch _watch = new Stopwatch();

        private bool _transformPoints;

        private void Start()
        {
            GenerateShape(false);
        }

        /// <summary>
        /// This method will be called in the editor script.
        /// </summary>
        public void GenerateShape(bool displayMessages = true)
        {
            if (displayMessages)
            {
                _watch.Start();
            }

            _transformPoints = true;

            // Get components if they are null
            if (_col == null)
            {
                if (TryGetComponent(out CompositeCollider2D compositeCol))
                {
                    _col = compositeCol;
                }
                else
                {
                    if (!TryGetComponent(out Collider2D col))
                    {
                        Debug.LogError($"{gameObject.name} must have a 2D Collider attached in order to be Tetrisified!", gameObject);
                        return;
                    }

                    _col = col;

                    if (col.GetType() == typeof(TetrisCollider))
                    {
                        _transformPoints = false;
                    }
                }
            }

            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            // Get the mesh
            Mesh mesh = meshFilter.sharedMesh;

            // Create a new one if the mesh filter has no mesh or if the mesh has read/write disabled
            if (mesh == null || !mesh.isReadable)
            {
                mesh = new Mesh();
                mesh.name = "Generated Mesh";
            }

            // Clear the mesh
            mesh.Clear();

            // Get the shapes from the 2D collider
            PhysicsShapeGroup2D shapes = new PhysicsShapeGroup2D();

            // Also make sure to store the amount of shapes
            int shapeAmount = _col.GetShapes(shapes);

            List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();
            List<Vector3[]> originalVertices = new List<Vector3[]>();

            // Loop through all the shapes
            for (int i = 0; i < shapeAmount; i++)
            {
                // Get the vertices of the shape at our current index
                List<Vector2> shapeVerts = new List<Vector2>();
                List<Vector3> originalVerticesList = new List<Vector3>();

                // This switch statement is to make sure circles and capsules will work
                PhysicsShape2D shape = shapes.GetShape(i);

                int count = Mathf.Max(Mathf.CeilToInt(shape.radius * 25), 25);
                float delta = Mathf.PI * 2 / count;

                switch (shape.shapeType)
                {
                    case PhysicsShapeType2D.Circle:
                        for (int circleIndex = 0; circleIndex < count; circleIndex++)
                        {
                            float x = Mathf.Cos(delta * circleIndex) * shape.radius;
                            float y = Mathf.Sin(delta * circleIndex) * shape.radius;

                            shapeVerts.Add(transform.TransformPoint(new Vector2(x, y) + _col.offset));
                        }
                        break;

                    case PhysicsShapeType2D.Edges:
                        Debug.LogWarning("Unable to Tetrisify Edge shapes sadly.");
                        break;

                    case PhysicsShapeType2D.Capsule:
                        Vector2 size = ((CapsuleCollider2D)_col).size;

                        for (int circleIndex = 0; circleIndex < count; circleIndex++)
                        {
                            float x = Mathf.Cos(delta * circleIndex) * shape.radius;
                            float y = Mathf.Sin(delta * circleIndex) * shape.radius;

                            y += (Mathf.Max(size.y / 2, shape.radius) - shape.radius) * Mathf.Sign(y);

                            shapeVerts.Add(transform.TransformPoint(new Vector2(x, y) + _col.offset));
                        }
                        break;

                    default:
                        shapes.GetShapeVertices(i, shapeVerts);
                        break;
                }

                int vertexAmount = shapeVerts.Count; // Cache the amount of vertices
                Vector2[] insetVertices = new Vector2[vertexAmount];

                Vector2 firstPoint = shapeVerts[0];
                Vector2 lastPoint = shapeVerts[vertexAmount - 1];

                Vector2 vertex;

                for (int vertIndex = 0; vertIndex < vertexAmount; vertIndex++)
                {
                    vertex = shapeVerts[vertIndex];

                    originalVerticesList.Add(vertex);

                    if (vertIndex == 0)
                    {
                        InsetCorner(lastPoint, vertex, shapeVerts[vertIndex + 1], DIST, ref vertex);
                    }
                    else if (vertIndex == vertexAmount - 1)
                    {
                        InsetCorner(shapeVerts[vertIndex - 1], vertex, firstPoint, DIST, ref vertex);
                    }
                    else
                    {
                        InsetCorner(shapeVerts[vertIndex - 1], vertex, shapeVerts[vertIndex + 1], DIST, ref vertex);
                    }

                    insetVertices[vertIndex] = vertex;

                    originalVerticesList.Add((Vector3)vertex + INSET_OFFSET);
                }

                originalVertices.Add(originalVerticesList.ToArray());

                triangles.AddRange(Polygon2Triangles(insetVertices));
            }

            int triangleCount = triangles.Count;
            int triangleVertexCount = triangleCount * 3;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triIndices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            Vector3 regDir = INSET_OFFSET.normalized;

            for (int i = 0; i < triangleCount; i++)
            {
                vertices.Add(Convert(triangles[i].Points._0));
                vertices.Add(Convert(triangles[i].Points._2));
                vertices.Add(Convert(triangles[i].Points._1));

                normals.Add(regDir);
                normals.Add(regDir);
                normals.Add(regDir);
            }

            for (int i = 0; i < triangleVertexCount; i++)
            {
                triIndices.Add(i);
            }

            int offset = vertices.Count;

            foreach (Vector3[] array in originalVertices)
            {
                Vector3 dir = Vector2.zero;
                Vector3 vertex = Vector2.zero;
                Vector3 nextVertex = Vector2.zero;

                // Use local methods dummy
                void SetVertex(int i)
                {
                    vertex = array[i];
                    nextVertex = array[i + 1];

                    dir = transform.InverseTransformDirection((vertex - nextVertex).normalized);
                }

                void AddVertex()
                {
                    vertices.Add(transform.InverseTransformPoint(vertex));
                    vertices.Add(transform.InverseTransformPoint(nextVertex));

                    normals.Add(dir);
                    normals.Add(dir);
                }

                int length = array.Length;

                for (int i = 0; i < length; i += 2)
                {
                    if (i > 0)
                    {
                        AddVertex();
                    }

                    SetVertex(i);

                    AddVertex();
                }

                SetVertex(length - 2);

                AddVertex();
            }

            void AddSquareTriangles(int a, int b, int c, int d)
            {
                triIndices.Add(a);
                triIndices.Add(b);
                triIndices.Add(d);

                triIndices.Add(d);
                triIndices.Add(c);
                triIndices.Add(a);
            }

            foreach (Vector3[] array in originalVertices)
            {
                int length = array.Length * 2;
                int offsetLength = offset + length;

                AddSquareTriangles(offset + 1, offset, offsetLength - 1, offsetLength - 2);

                for (int i = offset + 5; i < offsetLength; i += 4)
                {
                    AddSquareTriangles(i, i - 1, i - 2, i - 3);
                }

                offset += length;
            }

            Vector3[] totalVerticesArray = vertices.ToArray();
            int[] triIndicesArray = triIndices.ToArray();
            Vector3[] normalsArray = normals.ToArray();

            this.vertices = totalVerticesArray;
            this.triIndices = triIndicesArray;
            this.normals = normalsArray;

            mesh.vertices = totalVerticesArray;
            mesh.triangles = triIndicesArray;
            mesh.normals = normalsArray;

            mesh.RecalculateNormals();

            // Set the mesh
            meshFilter.mesh = mesh;

            if (displayMessages)
            {
                // Stop the Stopwatch
                _watch.Stop();

                // Debug log message of how long it took
                long time = _watch.ElapsedMilliseconds;
                Debug.Log("Generated mesh. It took: " + time + (time == 1 ? "millisecond." : " milliseconds."), gameObject);
            }
        }

        private IList<DelaunayTriangle> Polygon2Triangles(IEnumerable<Vector2> vertices)
        {
            List<PolygonPoint> polygonPoints = new List<PolygonPoint>();

            foreach (Vector2 vertex in vertices)
            {
                polygonPoints.Add(new PolygonPoint(vertex.x, vertex.y));
            }

            Polygon polygon = new Polygon(polygonPoints);

            P2T.Triangulate(polygon);

            return polygon.Triangles;
        }

        private Vector3 Convert(TriangulationPoint p)
        {
            return transform.InverseTransformPoint(new Vector3(p.Xf, p.Yf)) + INSET_OFFSET;
        }

        #region Inset Corner Method
        private static bool InsetCorner(Vector2 prevPoint, Vector2 currentPoint, Vector2 nextPoint, float insetDist, ref Vector2 result)
        {
            // Get the direction of both lines and make them perpendicular to eachother (to get a 90 degree offset)
            Vector2 line1Offset = Vector2.Perpendicular((currentPoint - prevPoint).normalized) * insetDist;
            Vector2 line2Offset = Vector2.Perpendicular((nextPoint - currentPoint).normalized) * insetDist;

            // Line 1
            Vector2 line1Pos1 = prevPoint + line1Offset;    // L1P1 = A
            Vector2 line1Pos2 = currentPoint + line1Offset; // L1P2 = B

            // Line 2
            Vector2 line2Pos1 = currentPoint + line2Offset; // L2P1 = C
            Vector2 line2Pos2 = nextPoint + line2Offset;    // L2P2 = D

            // Return the line pos if they both are the exact same line
            if (line1Pos1 == line2Pos1)
            {
                result = line1Pos1;
                return true;
            }

            // Now we just find where these 2 lines intersect eachother and we got ourselves our inset point

            // Line AB represented as a1x + b1y = c1
            float a1 = line1Pos2.y - line1Pos1.y;
            float b1 = line1Pos1.x - line1Pos2.x;
            float c1 = a1 * (line1Pos1.x) + b1 * (line1Pos1.y);

            // Line CD represented as a2x + b2y = c2
            float a2 = line2Pos2.y - line2Pos1.y;
            float b2 = line2Pos1.x - line2Pos2.x;
            float c2 = a2 * (line2Pos1.x) + b2 * (line2Pos1.y);

            float determinant = a1 * b2 - a2 * b1;

            if (determinant != 0)
            {
                result.x = (b2 * c1 - b1 * c2) / determinant;
                result.y = (a1 * c2 - a2 * c1) / determinant;

                return true;
            }

            return false;
        }
        #endregion

        #region Useless Copy Pasted Code Whoops
        /*
        /// <summary>
        /// Taken from: https://alienryderflex.com/polygon_inset/ and translated into C#.
        /// </summary>
        private static bool InsetCorner(Vector2 prevPoint, Vector2 currentPoint, Vector2 nextPoint, float insetDist, ref Vector2 result)
        {
            Vector2 d1 = currentPoint - prevPoint;
            Vector2 d2 = nextPoint - currentPoint;

            // Calculate length of line segments.
            float dist1 = Mathf.Sqrt(d1.x * d1.x + d1.y * d1.y);
            float dist2 = Mathf.Sqrt(d2.x * d2.x + d2.y * d2.y);

            // Exit if either segment is zero-length.
            if (dist1 == 0 || dist2 == 0)
            {
                return false;
            }

            // Inset each of the two line segments.
            Vector2 inset;

            inset = new Vector2(d1.y, d1.x) / dist1 * insetDist;

            prevPoint += inset;
            Vector2 newCurrentPoint1 = currentPoint + inset;

            inset = new Vector2(d2.y, d2.x) / dist2 * insetDist;

            nextPoint += inset;
            Vector2 newCurrentPoint2 = currentPoint + inset;

            // If inset segments connect perfectly, return the connection point.
            if (newCurrentPoint1 == newCurrentPoint2)
            {
                result = newCurrentPoint1;
                return true;
            }

            // Return the intersection point of the two inset segments (if any).
            if (LineIntersection(prevPoint, newCurrentPoint1, newCurrentPoint2, nextPoint, ref result))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Taken from: http://alienryderflex.com/intersect/ and translated into C#.
        /// </summary>
        private static bool LineIntersection(
            Vector2 pointA, Vector2 pointB, // Line 1
            Vector2 pointC, Vector2 pointD, // Line 2
            ref Vector2 result)
        {
            //  Fail if either line is undefined.
            if (pointA == pointB || pointC == pointD)
            {
                return false;
            }

            // (1) Translate the system so that point A is on the origin.
            pointB -= pointA;
            pointC -= pointA;
            pointD -= pointA;

            // Discover the length of segment A-B.
            float distAB = Mathf.Sqrt(pointB.x * pointB.x + pointB.y * pointB.y);

            // (2) Rotate the system so that point B is on the positive X axis.
            float theCos = pointB.x / distAB;
            float theSin = pointB.y / distAB;

            float newX = pointC.x * theCos + pointC.y * theSin;
            pointC.y = pointC.y * theCos - pointC.x * theSin;
            pointC.x = newX;

            newX = pointD.x * theCos + pointD.y * theSin;
            pointD.y = pointD.y * theCos - pointD.x * theSin;
            pointD.x = newX;

            // Fail if the lines are parallel.
            if (pointC.y == pointD.y)
            {
                return false;
            }

            // (3) Discover the position of the intersection point along line A-B.
            float ABpos = pointD.x + (pointC.x - pointD.x) * pointD.y / (pointD.y - pointC.y);

            // (4) Apply the discovered position to line A-B in the original coordinate system.
            result.x = pointA.x + ABpos * theCos;
            result.y = pointA.y + ABpos * theSin;

            // Success.
            return true;
        }
        */
        #endregion

        [CustomEditor(typeof(GenerateTetrisMesh))]
        public class GenerateTetrisMeshEditor : Editor
        {
            private GenerateTetrisMesh _target;

            private void OnEnable()
            {
                _target = (GenerateTetrisMesh)target;
            }

            public override void OnInspectorGUI()
            {
                if (GUILayout.Button("Generate Mesh"))
                {
                    _target.GenerateShape();
                }
            }
        }
#else
        private void Start()
        {
            // Get component if it's null
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            // Serialize the mesh
            Mesh mesh = new Mesh();
            mesh.name = "Generated Mesh";

            mesh.vertices = vertices;
            mesh.triangles = triIndices;
            mesh.normals = normals;

            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        public void GenerateShape()
        {
            
        }
#endif

        /*
        // Code for drawing normals
        private void OnDrawGizmosSelected()
        {
            // Draw normals
            Gizmos.color = new Color(0.0117647058823529f, 0.7294117647058824f, 0.9882352941176471f, 1);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = transform.TransformPoint(vertices[i]);
                Vector3 normal = transform.TransformDirection(normals[i]);

                Gizmos.DrawLine(vertex, vertex + (normal * 0.3f));
            }
        }
        */
    }
}