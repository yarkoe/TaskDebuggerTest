using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using TaskDebuggerTest;

namespace TestTaskDebuggerTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void CloneGraphTest()
        {
            var graph = new Graph<int>();
            graph.AddVertex(1);
            graph.AddVertex(500);
            graph.AddVertex(200);
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);

            var clonedGraph = graph.Clone();
            clonedGraph.AddVertex(2);
            clonedGraph.AddEdge(1, 4);
            
            Assert.True(graph.Edges.Count != clonedGraph.Edges.Count);
            Assert.True(graph.AdjacencyLists[0].Value.Count != clonedGraph.AdjacencyLists[0].Value.Count);
        }

        [Test]
        public void CycleGraphTest()
        {
            var actions = new Graph<Action>();
            for (var i = 0; i < 3; i++)
                actions.AddVertex(() => { });

            actions.AddEdge(1, 2);
            actions.AddEdge(2, 3);

            Assert.False(actions.HasCycle());

            actions.AddEdge(3, 1);
            Assert.True(actions.HasCycle());

            Assert.Catch(() => GraphParallelProcessor.ProcessInParallelAsync(actions));
        }
        
        [Test]
        public void SimpleAsyncTest()
        {
            var concurrentQueue = new ConcurrentQueue<int>();
            var firstAction = new Action(() => concurrentQueue.Enqueue(1));
            var secondAction = new Action(() => concurrentQueue.Enqueue(2));
            var thirdAction = new Action(() => concurrentQueue.Enqueue(3));

            var actions = new Graph<Action>();
            var firstVertex = actions.AddVertex(firstAction);
            var secondVertex = actions.AddVertex(secondAction);
            var thirdVertex = actions.AddVertex(thirdAction);

            actions.AddEdge(firstVertex.Id, thirdVertex.Id);
            actions.AddEdge(secondVertex.Id, thirdVertex.Id);

            var task = GraphParallelProcessor.ProcessInParallelAsync(actions);
            
            task.Wait();
            
            Assert.AreEqual(concurrentQueue.ToArray()[2], 3);
        }

        [Test]
        public void ComplexAsyncTest()
        {
            var concurrentQueue = new ConcurrentQueue<int>();
            var actions = new Graph<Action>();
            
            const int vertexNumber = 9;
            for (var i = 1; i <= vertexNumber; i++)
            {
                var iLocal = i;
                actions.AddVertex(() => concurrentQueue.Enqueue(iLocal));
            }
            
            Tuple<int, int>[] tupleEdges =
            {
                Tuple.Create(1, 4),
                Tuple.Create(1, 5),
                Tuple.Create(2, 4),
                Tuple.Create(3, 4),
                Tuple.Create(4, 6),
                Tuple.Create(4, 7),
                Tuple.Create(6, 5),
                Tuple.Create(7, 8),
                Tuple.Create(7, 9)
            };

            foreach (var (fromVertexId, toVertexId) in tupleEdges)
            {
                actions.AddEdge(fromVertexId, toVertexId);
            }
            
            GraphParallelProcessor.ProcessInParallelAsync(actions).Wait();

            var concurrentArray = concurrentQueue.ToArray();
            Assert.True(CheckItem1BeforeItem2InArray(1, 4, concurrentArray));
            Assert.True(CheckItem1BeforeItem2InArray(1, 5, concurrentArray));
            Assert.True(CheckItem1BeforeItem2InArray(4, 6, concurrentArray));
            Assert.True(CheckItem1BeforeItem2InArray(6, 5, concurrentArray));
            Assert.True(CheckItem1BeforeItem2InArray(4, 6, concurrentArray));
            Assert.True(CheckItem1BeforeItem2InArray(7, 8, concurrentArray));
            
            Assert.False(CheckItem1BeforeItem2InArray(5, 3, concurrentArray));
            Assert.False(CheckItem1BeforeItem2InArray(9, 4, concurrentArray));
        }

        private static bool CheckItem1BeforeItem2InArray(int item1, int item2, int[] array)
        {
            var index1 = Array.FindIndex(array, item => item1 == item);
            var index2 = Array.FindIndex(array, item => item2 == item);

            return index1 < index2;
        }
    }
}