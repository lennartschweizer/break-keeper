namespace Core;

public sealed class FocusSessionEngine
{
    private readonly TimeProvider _timeProvider;
    private readonly ISessionStore _store;
    private readonly IAuditLog _auditLog;

    public FocusSessionEngine(TimeProvider timeProvider, ISessionStore store, IAuditLog auditLog)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
    }

    public TimerPhase Phase { get; private set; } = TimerPhase.Idle;
    public TimerPreset ActivePreset { get; private set; } = TimerPreset.Deep50;
    public string? EntryStep { get; private set; }
    public DateTimeOffset? PhaseEndUtc { get; private set; }
    public int ExtensionsUsed { get; private set; }
    public string? EmergencyExitPhrase { get; private set; }

    public TimeSpan Remaining
    {
        get
        {
            if (Phase == TimerPhase.Idle || Phase == TimerPhase.BreakCompleted || PhaseEndUtc is null)
                return TimeSpan.Zero;

            TimeSpan delta = PhaseEndUtc.Value - _timeProvider.GetUtcNow();
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }
    }

    public bool CanExtend =>
        (Phase == TimerPhase.Focus || Phase == TimerPhase.PreWarning) &&
        ExtensionsUsed < ActivePreset.MaxExtensions;

    public void StartFocus(TimerPreset preset, string? entryStep = null)
    {
        ArgumentNullException.ThrowIfNull(preset);
        preset.Validate();

        if (Phase != TimerPhase.Idle)
            throw new InvalidOperationException($"Cannot start focus while in phase '{Phase}'.");

        DateTimeOffset now = _timeProvider.GetUtcNow();
        ActivePreset = preset;
        EntryStep = string.IsNullOrWhiteSpace(entryStep) ? null : entryStep.Trim();
        PhaseEndUtc = now + preset.FocusDuration;
        ExtensionsUsed = 0;
        EmergencyExitPhrase = null;
        Phase = preset.PreWarningDuration > TimeSpan.Zero && preset.FocusDuration <= preset.PreWarningDuration
            ? TimerPhase.PreWarning
            : TimerPhase.Focus;

        Persist();
    }

    public bool Tick()
    {
        if (Phase == TimerPhase.Idle || PhaseEndUtc is null)
            return false;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimerPhase previousPhase = Phase;

        if (Phase == TimerPhase.Focus || Phase == TimerPhase.PreWarning)
        {
            if (now >= PhaseEndUtc.Value)
            {
                DateTimeOffset breakEnd = PhaseEndUtc.Value + ActivePreset.BreakDuration;
                if (now >= breakEnd)
                {
                    TransitionToBreakCompleted();
                }
                else
                {
                    Phase = TimerPhase.Break;
                    PhaseEndUtc = breakEnd;
                    EmergencyExitPhrase = EmergencyExitChallenge.GeneratePhrase();
                }
                Persist();
                return true;
            }

            if (Phase == TimerPhase.Focus &&
                ActivePreset.PreWarningDuration > TimeSpan.Zero &&
                PhaseEndUtc.Value - now <= ActivePreset.PreWarningDuration)
            {
                Phase = TimerPhase.PreWarning;
                Persist();
                return true;
            }
        }
        else if (Phase == TimerPhase.Break && now >= PhaseEndUtc.Value)
        {
            TransitionToBreakCompleted();
            Persist();
            return true;
        }

        return Phase != previousPhase;
    }

    public bool TryExtendFocus()
    {
        if (!CanExtend || PhaseEndUtc is null)
            return false;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        PhaseEndUtc = PhaseEndUtc.Value + ActivePreset.ExtensionDuration;
        ExtensionsUsed++;

        if (ActivePreset.PreWarningDuration > TimeSpan.Zero &&
            PhaseEndUtc.Value - now > ActivePreset.PreWarningDuration)
        {
            Phase = TimerPhase.Focus;
        }

        Persist();
        return true;
    }

    public bool TryEmergencyExitBreak(string? typedPhrase, bool holdCompleted)
    {
        if (Phase != TimerPhase.Break || !holdCompleted)
            return false;

        if (!EmergencyExitChallenge.Matches(EmergencyExitPhrase, typedPhrase))
            return false;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        TimeSpan remaining = Remaining;
        _auditLog.AppendEmergencyExit(now, ActivePreset.Name, remaining);

        ResetToIdle();
        Persist();
        return true;
    }

    public void StartNextFocus()
    {
        if (Phase != TimerPhase.BreakCompleted)
            throw new InvalidOperationException($"Cannot start next focus while in phase '{Phase}'.");

        DateTimeOffset now = _timeProvider.GetUtcNow();
        EntryStep = null;
        PhaseEndUtc = now + ActivePreset.FocusDuration;
        ExtensionsUsed = 0;
        EmergencyExitPhrase = null;
        Phase = ActivePreset.PreWarningDuration > TimeSpan.Zero && ActivePreset.FocusDuration <= ActivePreset.PreWarningDuration
            ? TimerPhase.PreWarning
            : TimerPhase.Focus;

        Persist();
    }

    public void FinishCycle()
    {
        if (Phase != TimerPhase.BreakCompleted)
            return;

        ResetToIdle();
        Persist();
    }

    public void RestoreFromStore()
    {
        SessionSnapshot? snapshot = _store.Load();
        if (snapshot is null)
        {
            ResetToIdle();
            return;
        }

        ActivePreset = snapshot.ActivePreset;
        EntryStep = snapshot.EntryStep;
        PhaseEndUtc = snapshot.PhaseEndUtc;
        ExtensionsUsed = snapshot.ExtensionsUsed;
        EmergencyExitPhrase = snapshot.EmergencyExitPhrase;
        Phase = snapshot.Phase;

        Tick();
    }

    private void TransitionToBreakCompleted()
    {
        Phase = TimerPhase.BreakCompleted;
        EntryStep = null;
        PhaseEndUtc = null;
        ExtensionsUsed = 0;
        EmergencyExitPhrase = null;
    }

    private void ResetToIdle()
    {
        Phase = TimerPhase.Idle;
        EntryStep = null;
        PhaseEndUtc = null;
        ExtensionsUsed = 0;
        EmergencyExitPhrase = null;
    }

    private void Persist()
    {
        _store.Save(new SessionSnapshot(
            Phase,
            ActivePreset,
            EntryStep,
            PhaseEndUtc,
            ExtensionsUsed,
            EmergencyExitPhrase));
    }
}
