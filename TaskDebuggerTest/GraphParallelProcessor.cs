using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace TaskDebuggerTest
{
    public static class GraphParallelProcessor
    {
        private static object lockObject = new object();
        
        public static Task ProcessInParallelAsync(Graph<Action> actions)
        {
            var transposedActions = Graph<Action>.TransposeGraph(actions);
            var initialVertices = transposedActions.GetLeaves();

            var initialTasks = initialVertices.Select(initialVertex => 
                Task.Run(() => ProcessVertexInParallelAsync(actions, transposedActions, initialVertex))).ToList();

            return Task.WhenAll(initialTasks);
        }

        private static async Task ProcessVertexInParallelAsync(Graph<Action> actions, Graph<Action> transposedActions, Graph<Action>.Vertex vertex)
        {
            var actionTask = Task.Run(() => vertex.Value());

            await actionTask;

            var nextVertices = new List<Task>();
            foreach (var toVertex in actions.FindVertexAdjacencyList(vertex.Id).Value)
            {
                lock (lockObject)
                {
                    transposedActions.RemoveEdge(toVertex.Id, vertex.Id);

                    if (transposedActions.FindVertexAdjacencyList(toVertex.Id).Value.Count == 0)
                    {
                        nextVertices.Add(Task.Run(() =>
                            ProcessVertexInParallelAsync(actions, transposedActions, toVertex)));
                    }
                }
            }

            await Task.WhenAll(nextVertices);
        }
    }
}