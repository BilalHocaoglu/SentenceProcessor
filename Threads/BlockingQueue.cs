using System.Collections.Generic;
using System.Threading;

namespace Threads
{
    public class BlockingQueue<T>
    {
        private readonly Queue<T> _queue = new();

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);

                Monitor.Pulse(_queue);
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue);
                }
                return _queue.Dequeue();
            }
        }
    }
}