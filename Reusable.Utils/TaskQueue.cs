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
        private readonly int _maxParallelTasks;

        private readonly LinkedList<Task> _tasks;

        public TaskQueue(int maxParallelTasks)
        {
            _maxParallelTasks = maxParallelTasks;
            _tasks = new LinkedList<Task>();
        }

        public void Add(Task task)
        {
            lock (this)
            {
                while (_tasks.Count > 0)
                {
                    Task oldestTask = _tasks.First.Value;
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

                    _tasks.RemoveFirst();
                }

                _tasks.AddLast(task);
            }
        }

        public void WaitAll()
        {
            Task[] arrayOfTasks;
            lock (this)
            {
                arrayOfTasks = _tasks.ToArray();
            }

            Task.WaitAll(arrayOfTasks);
        }

    }// end of class TaskQueue

}// end of namespace Reusable.Utils
