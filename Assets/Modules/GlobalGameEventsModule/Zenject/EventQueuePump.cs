using Zenject;

namespace Game.Events.DI
{
    /// <summary>
    /// Flushes the deferred event queue once per frame, from Zenject's late tick.
    /// </summary>
    /// <remarks>
    /// Late tick means every <c>ITickable</c> and every MonoBehaviour <c>Update</c> has already run, so an event
    /// queued anywhere during the frame is delivered in the same frame, after the state that produced it has
    /// settled. Events queued from inside a handler roll over to the next frame; see
    /// <see cref="IEventQueue.Flush"/>.
    /// </remarks>
    internal sealed class EventQueuePump : ILateTickable
    {
        private readonly IEventQueue _queue;

        public EventQueuePump(IEventQueue queue)
        {
            _queue = queue;
        }

        public void LateTick()
        {
            _queue.Flush();
        }
    }
}
