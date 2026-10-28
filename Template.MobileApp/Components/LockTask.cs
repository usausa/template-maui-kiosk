namespace Template.MobileApp.Components;

public enum LockTaskState
{
    None,
    Locked,
    Pinned
}

public sealed partial class LockTask : IDisposable
{
    private const int RequestPolls = 30;

    private const int StopPolls = 20;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan StopPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly Lock sync = new();

    private readonly Timer timer;

    private LockTaskState watchedState;

    private int remainingPolls;

    public event EventHandler? Changed;

    public LockTaskState State => ResolveState();

    public bool IsPermitted => ResolvePermitted();

    // ------------------------------------------------------------
    // Control
    // ------------------------------------------------------------

    public void Start()
    {
        PlatformStart();
        Watch(RequestPolls);
    }

    public async ValueTask<bool> StopAsync()
    {
        if (State == LockTaskState.None)
        {
            return true;
        }

        PlatformStop();
        Watch(0);
        for (var i = 0; (i < StopPolls) && (State != LockTaskState.None); i++)
        {
            await Task.Delay(StopPollInterval);
        }

        return State == LockTaskState.None;
    }

    // ------------------------------------------------------------
    // Watch
    // ------------------------------------------------------------

    private void Watch(int polls)
    {
        lock (sync)
        {
            watchedState = State;
            remainingPolls = polls;
            timer.Change(PollInterval, PollInterval);
        }

        RaiseChanged();
    }

    private void Poll()
    {
        bool changed;
        lock (sync)
        {
            var state = State;
            changed = state != watchedState;
            watchedState = state;
            remainingPolls--;
            if ((state != LockTaskState.Pinned) && (remainingPolls <= 0))
            {
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        if (changed)
        {
            RaiseChanged();
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private partial LockTaskState ResolveState();

    private partial bool ResolvePermitted();

    private static partial void PlatformStart();

    private static partial void PlatformStop();
}
