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
            var transposedActionGraph = Graph<Action>.TransposeGraph(actions);
            var initialVertices = transposedActionGraph.GetLeaves();

            var initialTasks = initialVertices.Select(initialVertex => 
                Task.Run(() => ActionVertexInParallelAsync(actions, transposedActionGraph, initialVertex))).ToList();

            return Task.WhenAll(initialTasks);
        }

        private static async Task ActionVertexInParallelAsync(Graph<Action> actions, Graph<Action> transposedActions, Graph<Action>.Vertex vertex)
        {
            var actionTask = Task.Run(() => vertex.Value());

            await actionTask;

            var nextVertices = new List<Task>();
            foreach (var toVertex in actions.FindVertexAdjacencyList(vertex.Id))
            {
                lock (lockObject)
                {
                    transposedActions.RemoveEdge(toVertex.Id, vertex.Id);

                    if (transposedActions.FindVertexAdjacencyList(toVertex.Id).Count == 0)
                    {
                        nextVertices.Add(Task.Run(() =>
                            ActionVertexInParallelAsync(actions, transposedActions, toVertex)));
                    }
                }
            }

            await Task.WhenAll(nextVertices);
        }
    }
}