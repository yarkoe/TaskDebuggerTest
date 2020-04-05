using System.Collections.Generic;
using System.Linq;

namespace TaskDebuggerTest
{
    /// <summary>
    /// Класс представляет граф.
    /// </summary>
    /// <typeparam name="T">Тип значения графа.</typeparam>
    public class Graph<T>
    {
        public HashSet<Vertex> Vertices = new HashSet<Vertex>();
        public HashSet<Edge> Edges = new HashSet<Edge>();
        public List<AdjacencyList> AdjacencyLists = new List<AdjacencyList>();

        /// <summary>
        /// Добавить вершину в граф.
        /// </summary>
        /// <param name="value">Значение вершины.</param>
        /// <returns>Созданная вершина.</returns>
        public Vertex AddVertex(T value)
        {
            var newVertex = new Vertex(Vertices.Count + 1, value); 
            Vertices.Add(newVertex);
            AdjacencyLists.Add(new AdjacencyList(newVertex));

            return newVertex;
        }

        /// <summary>
        /// Искать вершины по id.
        /// </summary>
        /// <param name="id">id вершины, которую необходимо найти.</param>
        /// <returns>Найденная вершина.</returns>
        public Vertex FindVertex(int id) => Vertices.First(vertex => vertex.Id == id);

        /// <summary>
        /// Добавить ориентированное ребро в граф.
        /// </summary>
        /// <param name="fromVertexId">id вершины, из которой опускается ребро.</param>
        /// <param name="toVertexId">id вершины, в которую входит ребро.</param>
        /// <returns>true, если ребро добавилось успешно, false в противном случае.</returns>
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
        
        /// <summary>
        /// Удалить ребро из графа.
        /// </summary>
        /// <param name="fromVertexId">id вершины, из которой опускается ребро.</param>
        /// <param name="toVertexId">id вершины, в которую входит ребро.</param>
        /// <returns>true, если ребро удалилось успешно, false в противном случае.</returns>
        public bool RemoveEdge(int fromVertexId, int toVertexId)
        {
            var fromVertex = FindVertex(fromVertexId);
            var toVertex = FindVertex(toVertexId);
            if (fromVertex == null || toVertex == null)
                return false ;

            Edges.RemoveWhere(edge => edge.FromVertex.Id == fromVertexId && edge.ToVertex.Id == toVertexId);
            
            var adjList = FindVertexAdjacencyList(fromVertexId);
            adjList.Value.RemoveAll(vertex => vertex.Id == toVertexId);

            return true;
        }

        /// <summary>
        /// Найти список смежности по id вершины.
        /// </summary>
        /// <param name="id">id вершины.</param>
        /// <returns>Список смежности.</returns>
        public AdjacencyList FindVertexAdjacencyList(int id) => AdjacencyLists.Find(adj => adj.RootVertex.Id == id);

        /// <summary>
        /// Клонировать граф.
        /// </summary>
        /// <returns>Клонированный граф.</returns>
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

        /// <summary>
        /// Проверить, есть ли в графе цикл.
        /// </summary>
        /// <returns>true, если цикл есть, false в противном случае.</returns>
        public bool HasCycle()
        {
            var vertexColor = Vertices.ToDictionary<Vertex, int, byte>(vertex => vertex.Id, vertex => 0);

            foreach (var vertex in Vertices)
                if (vertexColor[vertex.Id] == 0)
                {
                    if (DepthCycleSearch(vertex, vertexColor))
                    {
                        return true;
                    }
                }

            return false;
        }

        /// <summary>
        /// Обойти в глубину с проверкой на цикличность графа.
        /// </summary>
        /// <param name="vertex">Текущая вершина.</param>
        /// <param name="vertexColor">Информация об обходе.</param>
        /// <returns>true, если в этом обходе существует цикл, false в противном случае.</returns>
        private bool DepthCycleSearch(Vertex vertex, IDictionary<int, byte> vertexColor)
        {
            vertexColor[vertex.Id] = 1;

            foreach (var curVertex in FindVertexAdjacencyList(vertex.Id).Value)
            {
                if (vertexColor[curVertex.Id] == 1)
                    return true;

                if (DepthCycleSearch(curVertex, vertexColor))
                    return true;
            }

            vertexColor[vertex.Id] = 2;

            return false;
        }

        /// <summary>
        /// Транспонировать граф.
        /// </summary>
        /// <param name="graph">Исходный граф.</param>
        /// <returns>Транспонированный граф.</returns>
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

        /// <summary>
        /// Вернуть листья графа.
        /// </summary>
        /// <returns>Список листьев графа.</returns>
        public List<Vertex> GetLeaves()
        {
            var leaves = new List<Vertex>();
            
            AdjacencyLists.ForEach(adjList =>
            {
                if (adjList.Value.Count == 0) leaves.Add(adjList.RootVertex);
            });

            return leaves;
        }

        /// <summary>
        /// Класс, описывающий вершину графа.
        /// </summary>
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

        /// <summary>
        /// Класс, описывающий ориентированное ребро графа.
        /// </summary>
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

        /// <summary>
        /// Класс, описывающий список смежности графа.
        /// </summary>
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