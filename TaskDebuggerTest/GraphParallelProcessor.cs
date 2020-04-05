using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaskDebuggerTest
{
    public static class GraphParallelProcessor
    {
        private static readonly MyTaskScheduler _taskScheduler = new MyTaskScheduler(); 
        
        private static readonly object _lockObject = new object();
        
        public static Task ProcessInParallelAsync(Graph<Action> actions)
        {
            if (actions.HasCycle())
                throw new Exception("Action graph has cycle");
            
            var transposedActions = Graph<Action>.TransposeGraph(actions);
            var initialVertices = transposedActions.GetLeaves();

            var initialTasks = initialVertices.Select(initialVertex => 
                Task.Factory.StartNew(() => 
                    ProcessVertexInParallelAsync(actions, transposedActions, initialVertex),
                    CancellationToken.None, TaskCreationOptions.None, _taskScheduler).Unwrap()).ToList();

            return Task.WhenAll(initialTasks);
        }

        private static async Task ProcessVertexInParallelAsync(Graph<Action> actions, Graph<Action> transposedActions, Graph<Action>.Vertex vertex)
        {
            var actionTask = Task.Factory.StartNew(() => vertex.Value(), 
                CancellationToken.None, TaskCreationOptions.None, _taskScheduler);

            await actionTask;

            var nextVertices = new List<Task>();
            foreach (var toVertex in actions.FindVertexAdjacencyList(vertex.Id).Value)
            {
                lock (_lockObject)
                {
                    transposedActions.RemoveEdge(toVertex.Id, vertex.Id);

                    if (transposedActions.FindVertexAdjacencyList(toVertex.Id).Value.Count == 0)
                    {
                        nextVertices.Add(Task.Factory.StartNew(() =>
                            ProcessVertexInParallelAsync(actions, transposedActions, toVertex),
                            CancellationToken.None, TaskCreationOptions.None, _taskScheduler).Unwrap());
                    }
                }
            }

            await Task.WhenAll(nextVertices);
        }
    }
}