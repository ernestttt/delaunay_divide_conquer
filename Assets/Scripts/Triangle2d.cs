using System.Linq;
using UnityEngine;

namespace MeshSlicing.Delaunay
{
    public class Triangle2d
    {
        private Edge2d _edge1;
        private Edge2d _edge2;
        private Edge2d _edge3;

        // hashes later
        private Edge2d[] _edges;

        private Vertex2d[] _vertices;
        public int[] Indices => _vertices.Select(a => a.Index).ToArray();

        public Triangle2d(Edge2d edge1, Edge2d edge2, Edge2d edge3)
        {
            _edge1 = edge1;
            _edge2 = edge2;
            _edge3 = edge3;
            _edges = new Edge2d[] { _edge1, _edge2, _edge3 };

            CreateVertices();
        }

        private void CreateVertices()
        {
            _vertices = new Vertex2d[3];

            Vertex2d vertex1;
            Vertex2d vertex2;
            Vertex2d vertex3;

            _edge1.HasSameVertexWith(_edge2, out vertex2);
            _edge2.HasSameVertexWith(_edge3, out vertex3);
            _edge3.HasSameVertexWith(_edge1, out vertex1);
            
            _vertices[0] = vertex1;
            _vertices[1] = vertex2;
            _vertices[2] = vertex3;
        }

        public bool IsSameTriangle(Edge2d edge1, Edge2d edge2, Edge2d edge3)
        {
            bool isSame = _edges.Contains(edge1) && _edges.Contains(edge2) && _edges.Contains(edge3);

            return isSame;
        }

        public void DebugPoints()
        {
            int[] orderedIndices = _vertices.OrderBy(a => a.Index).Select(a => a.Index).ToArray();
            Debug.Log($"({orderedIndices[0]}, {orderedIndices[1]}, {orderedIndices[2]})");
        }

        public bool IsAbnormal(){
            _edge1.HasSameVertexWith(_edge2, out Vertex2d commonVertex);
            Vector2 v1 = _edge1.GetVectorFromEdge(commonVertex);
            Vector2 v2 = _edge2.GetVectorFromEdge(commonVertex);

            float angle = Vector2.Angle(v1, v2);
            return angle < 0.01 || angle > 179.001;
        }

        public void DebugVisualize(){
            foreach(var edge in _edges){
                edge.DebugVisualize();
            }
        }
    }
}