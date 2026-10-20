using System.Collections.Generic;

namespace Game.Events
{
    /// <summary>A single live subscription as the debugger window sees it.</summary>
    public readonly struct SubscriptionInfo
    {
        /// <summary>Event path for a typed subscription, branch path for a branch subscription.</summary>
        public readonly string Path;

        /// <summary>True when this is a branch subscription.</summary>
        public readonly bool IsBranch;

        /// <summary>Dispatch priority. Lower runs first.</summary>
        public readonly int Priority;

        /// <summary>Description of the object that owns the handler.</summary>
        public readonly string Target;

        /// <summary>Creates the record.</summary>
        public SubscriptionInfo(string path, bool isBranch, int priority, string target)
        {
            Path = path;
            IsBranch = isBranch;
            Priority = priority;
            Target = target;
        }
    }

    /// <summary>
    /// Read only view of what the service currently holds. Bound alongside <see cref="IGlobalGameEvents"/> so the editor
    /// window can resolve it from a context container.
    /// </summary>
    public interface IGameEventsDiagnostics
    {
        /// <summary>Total number of live typed and branch subscriptions.</summary>
        int SubscriptionCount { get; }

        /// <summary>Events waiting in the deferred queue.</summary>
        int PendingEventCount { get; }

        /// <summary>How deep the current dispatch is nested. Zero when idle.</summary>
        int DispatchDepth { get; }

        /// <summary>
        /// Fills <paramref name="buffer"/> with one entry per live subscription.
        /// </summary>
        /// <param name="buffer">Cleared before it is filled. Reuse it across frames to avoid garbage.</param>
        void CopySubscriptions(List<SubscriptionInfo> buffer);
    }
}
