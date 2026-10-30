namespace Template.MobileApp.Components;

using Android.App;

using AndroidX.Enterprise.Feedback;

public sealed partial class AppFeedback
{
    private readonly KeyedAppStatesReporter reporter = KeyedAppStatesReporter.Create(Application.Context);

    public void Dispose() => reporter.Dispose();

    private static KeyedAppState CreateKeyedAppState(AppState state)
    {
        using var builder = KeyedAppState.Builder();
        return builder
            .SetKey(state.Key)
            .SetSeverity(state.Severity == AppStateSeverity.Error ? KeyedAppState.SeverityError : KeyedAppState.SeverityInfo)
            .SetMessage(state.Message)
            .SetData(state.Data)
            .Build();
    }

    private partial void PlatformReport(IEnumerable<AppState> states)
    {
        var keyedStates = states.Select(CreateKeyedAppState).ToList();
        try
        {
            reporter.SetStates(keyedStates, null);
        }
        finally
        {
            foreach (var keyedState in keyedStates)
            {
                keyedState.Dispose();
            }
        }
    }
}
