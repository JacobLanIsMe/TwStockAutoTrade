using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Model
{
    public class BlockingQueue<T>
    {
        private BlockingCollection<T> _messageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
        public BlockingQueue(Action<T> ProcessMessage)
        {
            Task.Run(() =>
            {
                foreach (T order in _messageQueue.GetConsumingEnumerable())
                {
                    ProcessMessage(order);
                }
            });
        }
        public void Add(T order)
        {
            _messageQueue.Add(order);
        }
    }
}
