namespace Order.API.Messages;

public interface IMessageQueue
{
    void Enqueue<T>(T message);
    T? Dequeue<T>();
}
