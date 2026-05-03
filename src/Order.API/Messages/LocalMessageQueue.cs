using System.Collections.Concurrent;

namespace Order.API.Messages;

public class LocalMessageQueue : IMessageQueue
{
    private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _queues = new();

    public void Enqueue<T>(T message)
    {
        if (message is null) return;
        _queues.GetOrAdd(typeof(T), _ => new ConcurrentQueue<object>()).Enqueue(message);
    }

    public T? Dequeue<T>()
    {
        if (_queues.TryGetValue(typeof(T), out var queue) && queue.TryDequeue(out var item))
            return (T)item;
        return default;
    }
}
