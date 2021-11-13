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

        /// <summary>
        /// Erstellt eine neue Instanz von <see cref="TaskQueue"/>.
        /// </summary>
        /// <param name="maxParallelTasks">Legt die maximale Anzahl von parallel läufenden Aufgaben.</param>
        public TaskQueue(ushort maxParallelTasks)
        {
            _maxParallelTasks = maxParallelTasks;
            _tasks = new Queue<Task>();
        }

        /// <summary>
        /// Fügt der Warteschlange eine neue Aufgabe hinzu.
        /// </summary>
        /// <param name="task">Die hinzufügende Aufgabe.</param>
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

        /// <summary>
        /// Wartet auf den Abschluss von allen Aufgaben in der Warteschlange.
        /// </summary>
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
