using FocusEnforcer.Core;

namespace FocusEnforcer.App;

public sealed class BreakOverlayCoordinator : IDisposable
{
    private readonly List<BreakOverlayWindow> _overlays = [];
    private readonly BreakKeyboardHook _keyboardHook = new();

    public bool IsActive => _overlays.Count > 0;

    public void SyncWithEngine(FocusSessionEngine engine, Action onStateChanged)
    {
        bool showOverlay = engine.Phase == TimerPhase.Break || engine.Phase == TimerPhase.BreakCompleted;
        if (!showOverlay)
        {
            if (_overlays.Count > 0)
                CloseAll();
            return;
        }

        bool isBreakCompleted = engine.Phase == TimerPhase.BreakCompleted;
        if (isBreakCompleted)
        {
            _keyboardHook.Uninstall();
        }

        if (_overlays.Count == 0)
        {
            string phrase = engine.EmergencyExitPhrase ?? string.Empty;
            IReadOnlyList<MonitorRect> monitors = MonitorBoundsProvider.GetMonitors();
            foreach (MonitorRect monitor in monitors)
            {
                var overlay = new BreakOverlayWindow(
                    monitor,
                    phrase,
                    (typedPhrase, holdCompleted) =>
                    {
                        if (engine.TryEmergencyExitBreak(typedPhrase, holdCompleted))
                        {
                            CloseAll();
                            onStateChanged();
                        }
                    },
                    () =>
                    {
                        engine.StartNextFocus();
                        CloseAll();
                        onStateChanged();
                    },
                    () =>
                    {
                        engine.FinishCycle();
                        CloseAll();
                        onStateChanged();
                    });
                overlay.UpdateView(isBreakCompleted, engine.Remaining);
                overlay.Show();
                overlay.PositionOnTargetMonitor();
                _overlays.Add(overlay);
            }

            if (!isBreakCompleted)
                _keyboardHook.Install();
        }
        else
        {
            foreach (BreakOverlayWindow overlay in _overlays)
            {
                overlay.UpdateView(isBreakCompleted, engine.Remaining);
            }
        }
    }

    public void CloseAll()
    {
        _keyboardHook.Uninstall();

        foreach (BreakOverlayWindow overlay in _overlays)
        {
            overlay.ForceClose();
        }
        _overlays.Clear();
    }

    public void Dispose()
    {
        CloseAll();
        _keyboardHook.Dispose();
    }
}
