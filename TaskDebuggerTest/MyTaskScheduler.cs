using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TaskDebuggerTest
{
    public class MyTaskScheduler : TaskScheduler, IDisposable
    {
        private const int ThreadNumber = 20;
        private readonly List<Task> _scheduledTasks = new List<Task>();
        private readonly MyThreadPool _threadPool;

        public MyTaskScheduler()
        {
            _threadPool = new MyThreadPool(ThreadNumber, this);
        }
        
        protected override void QueueTask(Task task)
        {
            _scheduledTasks.Add(task);
            _threadPool.AddTask(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        protected override IEnumerable<Task> GetScheduledTasks() => _scheduledTasks;

        public void Dispose() => _threadPool.Shutdown();

        private class MyThreadPool
        {
            private readonly object _lockObject = new object();
            private readonly object _eventLockObject = new object();

            private CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationToken _token;

            private readonly int _threadCount = 0;
            private int _stopCount = 0;

            private readonly ConcurrentQueue<Task> _taskQueue = new ConcurrentQueue<Task>();
            private Thread[] _threads;
            private readonly ManualResetEvent _threadEvent = new ManualResetEvent(false);
            private readonly ManualResetEvent _allstopEvent = new ManualResetEvent(false);

            private readonly MyTaskScheduler _myTaskScheduler;
            
            public MyThreadPool(int n, MyTaskScheduler taskScheduler)
            {
                _myTaskScheduler = taskScheduler;
                
                _threadCount = n;

                _token = _cts.Token;

                _threads = new Thread[_threadCount];

                for (var i = 0; i < _threadCount; ++i)
                {
                    _threads[i] = new Thread(Run) { IsBackground = true };

                }

                foreach (var thread in _threads)
                {
                    thread.Start();
                }
            }
            
            public void AddTask(Task task)
            {
                AddToQueue(task);
            }
            
            private void AddToQueue(Task task)
            {
                lock (_eventLockObject)
                {
                    _taskQueue.Enqueue(task);

                    _threadEvent.Set();
                }
            }
            
            private void Run()
            {
                while (true)
                {
                    _threadEvent.WaitOne();

                    lock (_eventLockObject)
                    {
                        if (_taskQueue.Count == 1)
                        {
                            _threadEvent.Reset();
                        }
                    }

                    if (_token.IsCancellationRequested)
                    {
                        _threadEvent.Set();

                        lock (_lockObject)
                        {
                            _stopCount++;
                        }

                        if (_stopCount == _threadCount)
                            _allstopEvent.Set();

                        return;
                    }

                    Task task = null;
                    lock (_lockObject)
                    {
                        if (_taskQueue.Count != 0)
                        {
                            _taskQueue.TryDequeue(out task);
                        }
                    }

                    if (task == null) continue;
                    
                    
                    _myTaskScheduler.TryExecuteTask(task);
                }
            }

            public void Shutdown()
            {
                _cts.Cancel();
                _cts.Dispose();

                _threadEvent.Set();

                _allstopEvent.WaitOne();
            }
        }
    }
}