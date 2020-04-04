using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TaskDebuggerTest
{
    public class Graph<T>
    {
        public HashSet<Vertex> Vertices = new HashSet<Vertex>();
        public HashSet<Edge> Edges = new HashSet<Edge>();
        public List<AdjacencyList> AdjacencyLists = new List<AdjacencyList>();

        public Vertex AddVertex(T value)
        {
            var newVertex = new Vertex(Vertices.Count + 1, value); 
            Vertices.Add(newVertex);
            AdjacencyLists.Add(new AdjacencyList(newVertex));

            return newVertex;
        }

        public Vertex FindVertex(int id) => Vertices.First(vertex => vertex.Id == id);

        public bool AddEdge(int fromVertexId, int toVertexId)
        {
            var fromVertex = FindVertex(fromVertexId);
            var toVertex = FindVertex(toVertexId);
            if (fromVertex == null || toVertex == null)
                return false;
            
            Edges.Add(new Edge(fromVertex, toVertex));
            
            var fromVertexAdjacencyList = FindVertexAdjacencyList(fromVertex.Id);
            fromVertexAdjacencyList.Value.Add(toVertex);

            return true;
        }
        
        public void RemoveEdge(int fromVertexId, int toVertexId)
        {
            var fromVertex = FindVertex(fromVertexId);
            var toVertex = FindVertex(toVertexId);
            if (fromVertex == null || toVertex == null)
                return;

            Edges.RemoveWhere(edge => edge.FromVertex.Id == fromVertexId && edge.ToVertex.Id == toVertexId);
            
            var adjList = FindVertexAdjacencyList(fromVertexId);
            adjList.Value.RemoveAll(vertex => vertex.Id == toVertexId);
        }

        public AdjacencyList FindVertexAdjacencyList(int id) => AdjacencyLists.Find(adj => adj.RootVertex.Id == id);

        public Graph<T> Clone()
        {
            var clonedGraph = new Graph<T>
            {
                Vertices = new HashSet<Vertex>(this.Vertices),
                Edges = new HashSet<Edge>(this.Edges),
                AdjacencyLists = new List<AdjacencyList>(this.AdjacencyLists.Capacity)
            };

            for (var i = 0; i < this.AdjacencyLists.Count; i++)
            {
                clonedGraph.AdjacencyLists.Add(new AdjacencyList(this.AdjacencyLists[i].RootVertex));
                clonedGraph.AdjacencyLists[i].Value = new List<Vertex>(this.AdjacencyLists[i].Value);
            }

            return clonedGraph;
        }

        public static Graph<T> TransposeGraph(Graph<T> graph)
        {
            var transposedGraph = graph.Clone();
            transposedGraph.Edges = new HashSet<Edge>();

            foreach (var adjList in transposedGraph.AdjacencyLists)
            {
                adjList.Value = new List<Vertex>();
            }
            
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
                if (adjList.Value.Count == 0) leaves.Add(adjList.RootVertex);
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

        public class Edge
        {
            public readonly Vertex FromVertex;
            public readonly Vertex ToVertex;
            
            public Edge(Vertex fromVertex, Vertex toVertex)
            {
                FromVertex = fromVertex;
                ToVertex = toVertex;
            }
        }

        public class AdjacencyList
        {
            public readonly Vertex RootVertex;
            public List<Vertex> Value = new List<Vertex>(); 

            public AdjacencyList(Vertex rootVertex)
            {
                RootVertex = rootVertex;
            }
        }
    }
}