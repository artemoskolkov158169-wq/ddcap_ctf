using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DDCAP.AltayCTF
{
    public class AsyncBoundedQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _writeSem;
        private readonly SemaphoreSlim _readSem;
        private volatile bool _completed;

        public AsyncBoundedQueue(int capacity)
        {
            _writeSem = new SemaphoreSlim(capacity, capacity);
            _readSem = new SemaphoreSlim(0, capacity);
        }

        public async Task WriteAsync(T item)
        {
            if (_completed) return;
            await _writeSem.WaitAsync().ConfigureAwait(false);
            if (_completed) { _writeSem.Release(); return; }
            _queue.Enqueue(item);
            _readSem.Release();
        }

        public bool TryWrite(T item)
        {
            if (_completed) return false;
            if (_writeSem.Wait(0))
            {
                if (_completed) { _writeSem.Release(); return false; }
                _queue.Enqueue(item);
                _readSem.Release();
                return true;
            }
            return false;
        }

        public async Task<T> ReadAsync(int millisecondsTimeout = -1)
        {
            bool entered = await _readSem.WaitAsync(millisecondsTimeout).ConfigureAwait(false);
            if (!entered) return default(T);

            T item;
            if (_queue.TryDequeue(out item))
            {
                _writeSem.Release();
                return item;
            }
            return default(T);
        }

        public void Complete()
        {
            _completed = true;
            try { _readSem.Release(1000); } catch { }
            try { _writeSem.Release(1000); } catch { }
        }

        public int Count { get { return _queue.Count; } }
    }
}
