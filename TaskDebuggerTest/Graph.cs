using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskDebuggerTest
{
    public class Graph<T>
    {
        public HashSet<Vertex> Vertices = new HashSet<Vertex>(new VertexComparer());
        public HashSet<Edge> Edges = new HashSet<Edge>(new EdgeComparer());
        public List<AdjacencyList> AdjacencyLists = new List<AdjacencyList>();

        public Vertex AddVertex(T value)
        {
            var newVertex = new Vertex(Vertices.Count + 1, value); 
            Vertices.Add(newVertex);
            AdjacencyLists.Add(new AdjacencyList(newVertex));

            return newVertex;
        }

        public bool HasVertex(int id) => Vertices.Any(curVertex => curVertex.Id == id);

        public Vertex FindVertex(int id) => Vertices.First(vertex => vertex.Id == id);

        public bool AddEdge(int fromVertexId, int toVertexId)
        {
            var fromVertex = FindVertex(fromVertexId);
            var toVertex = FindVertex(toVertexId);
            if (fromVertex == null || toVertex == null)
                return false;
            
            Edges.Add(new Edge(fromVertex, toVertex));
            
            var fromVertexAdjacencyList = FindVertexAdjacencyList(fromVertex.Id);
            fromVertexAdjacencyList.Add(toVertex);

            return true;
        }
        
        public void RemoveEdge(int fromVertexId, int toVertexId)
        {
            var fromVertex = FindVertex(fromVertexId);
            var toVertex = FindVertex(toVertexId);
            if (fromVertex == null || toVertex == null)
                return;

            Edges.Remove(new Edge(fromVertex, toVertex));
            var adjList = FindVertexAdjacencyList(fromVertexId);
            adjList.Remove(FindVertex(toVertexId));
        }

        public AdjacencyList FindVertexAdjacencyList(int id) => AdjacencyLists.Find(adjList => adjList.RootVertex.Id == id);

        public Graph<T> Clone()
        {
            var clonedGraph = this;
            clonedGraph.Vertices = new HashSet<Vertex>(this.Vertices);
            clonedGraph.Edges = new HashSet<Edge>(this.Edges);
            clonedGraph.AdjacencyLists = new List<AdjacencyList>(this.AdjacencyLists);

            return clonedGraph;
        }

        public static Graph<T> TransposeGraph(Graph<T> graph)
        {
            var transposedGraph = graph.Clone();
            transposedGraph.Edges = new HashSet<Edge>();
            transposedGraph.AdjacencyLists = new List<AdjacencyList>();
            
            foreach (var edge in graph.Edges)
            {
                transposedGraph.AddEdge(edge.ToVertex.Id, edge.FromVertex.Id);
            }
                
            return transposedGraph;
        }

        public List<Vertex> GetLeaves()
        {
            var leaves = new List<Vertex>();
            
            AdjacencyLists.ForEach(adjList =>
            {
                if (adjList.Count == 0) leaves.Add(adjList.RootVertex);
            });

            return leaves;
        }

        public class Vertex
        {
            public int Id { get; }
            public T Value { get; }

            public Vertex(int id, T value)
            {
                Id = id;
                Value = value;
            }
        }

        public class VertexComparer : IEqualityComparer<Vertex>
        {
            public bool Equals(Vertex vertex1, Vertex vertex2)
            {
                if (vertex1 == null && vertex2 == null) return true;
                if (vertex1 == null || vertex2 == null) return false;
                return vertex1.Id == vertex2.Id;
            }

            public int GetHashCode(Vertex vertex)
            {
                var code = vertex.Id + "|" + vertex.Value;
                return code.GetHashCode();
            }
        }

        public class Edge
        {
            public Vertex FromVertex;
            public Vertex ToVertex;
            
            public Edge(Vertex fromVertex, Vertex toVertex)
            {
                FromVertex = fromVertex;
                ToVertex = toVertex;
            }
        }

        public class EdgeComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge edge1, Edge edge2)
            {
                if (edge1 == null && edge2 == null) return true;
                if (edge1 == null || edge2 == null) return false;
                return edge1.FromVertex.Id == edge2.FromVertex.Id && edge1.ToVertex.Id == edge2.ToVertex.Id;
            }

            public int GetHashCode(Edge edge)
            {
                var code = edge.FromVertex.Id + "|" + edge.FromVertex.Value + "," + edge.ToVertex.Id + "|" +
                           edge.ToVertex.Value;
                return code.GetHashCode();
            }
        }
        
        public class AdjacencyList : List<Vertex>
        {
            public Vertex RootVertex;

            public AdjacencyList(Vertex rootVertex)
            {
                RootVertex = rootVertex;
            }
        }
    }
}