using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reusable.Utils
{
    /// <summary>
    /// Hält eine Sammlung von Aufgaben.
    /// Wenn es zu viele laufende Aufgaben gibt, diese Implementierung
    /// blockiert, bis eine Aufgabe zum Ende kommt, bevor eine andere
    /// hinzugefügt werden kann.
    /// </summary>
    public class TaskQueue
    {
        private readonly ushort _maxParallelTasks;

        private readonly Queue<Task> _tasks;

        public TaskQueue(ushort maxParallelTasks)
        {
            _maxParallelTasks = maxParallelTasks;
            _tasks = new Queue<Task>();
        }

        public void Add(Task task)
        {
            lock (this)
            {
                while (_tasks.Count > 0)
                {
                    Task oldestTask = _tasks.Peek();
                    if (!oldestTask.IsCompleted)
                    {
                        if (_tasks.Count == _maxParallelTasks)
                        {
                            oldestTask.Wait();
                        }
                        else
                        {
                            break;
                        }
                    }

                    _tasks.Dequeue();
                }

                _tasks.Enqueue(task);
            }
        }

        public void WaitAll()
        {
            while (true)
            {
                Task[] arrayOfTasks;
                lock (this)
                {
                    if (_tasks.Count == 0)
                        return;

                    arrayOfTasks = _tasks.ToArray();
                    _tasks.Clear();
                }

                Task.WaitAll(arrayOfTasks);
            }
        }

    }// end of class TaskQueue

}// end of namespace Reusable.Utils
