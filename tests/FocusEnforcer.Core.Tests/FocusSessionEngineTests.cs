using FocusEnforcer.Core;

namespace FocusEnforcer.Core.Tests;

public sealed class FocusSessionEngineTests
{
    private sealed class FakeTimeProvider(DateTimeOffset initialUtc) : TimeProvider
    {
        private DateTimeOffset _utcNow = initialUtc;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow += delta;
    }

    private sealed class InMemoryAuditLog : IAuditLog
    {
        public List<(DateTimeOffset TimestampUtc, string PresetName, TimeSpan RemainingBreak)> Entries { get; } = [];

        public void AppendEmergencyExit(DateTimeOffset timestampUtc, string presetName, TimeSpan remainingBreak) =>
            Entries.Add((timestampUtc, presetName, remainingBreak));
    }

    private static (FocusSessionEngine Engine, FakeTimeProvider Time, JsonSessionStore Store, InMemoryAuditLog Audit, string TempDir) CreateHarness()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"focus-enforcer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        var store = new JsonSessionStore(Path.Combine(tempDir, "session.json"));
        var audit = new InMemoryAuditLog();
        var engine = new FocusSessionEngine(time, store, audit);
        return (engine, time, store, audit, tempDir);
    }

    [Fact]
    public void FullCycle_TransitionsDeterministically_FromIdleToFocusPreWarningBreakAndBackToIdle()
    {
        var (engine, time, _, _, tempDir) = CreateHarness();
        try
        {
            engine.StartFocus(TimerPreset.Deep50, "  Write one paragraph  ");
            Assert.Equal(TimerPhase.Focus, engine.Phase);
            Assert.Equal("Write one paragraph", engine.EntryStep);
            Assert.Equal(TimeSpan.FromMinutes(50), engine.Remaining);

            time.Advance(TimeSpan.FromMinutes(48));
            Assert.True(engine.Tick());
            Assert.Equal(TimerPhase.PreWarning, engine.Phase);
            Assert.Equal(TimeSpan.FromMinutes(2), engine.Remaining);

            time.Advance(TimeSpan.FromMinutes(2));
            Assert.True(engine.Tick());
            Assert.Equal(TimerPhase.Break, engine.Phase);
            Assert.Equal(TimeSpan.FromMinutes(10), engine.Remaining);
            Assert.NotNull(engine.EmergencyExitPhrase);
            Assert.Equal(EmergencyExitChallenge.PhraseLength, engine.EmergencyExitPhrase!.Length);

            time.Advance(TimeSpan.FromMinutes(10));
            Assert.True(engine.Tick());
            Assert.Equal(TimerPhase.BreakCompleted, engine.Phase);
            Assert.Null(engine.EmergencyExitPhrase);
            Assert.Null(engine.EntryStep);

            engine.StartNextFocus();
            Assert.Equal(TimerPhase.Focus, engine.Phase);
            Assert.Equal(TimeSpan.FromMinutes(50), engine.Remaining);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryExtendFocus_AllowsAtMostOneExtension_AndReturnsFromPreWarningToFocus()
    {
        var (engine, time, _, _, tempDir) = CreateHarness();
        try
        {
            engine.StartFocus(TimerPreset.Starter25);
            time.Advance(TimeSpan.FromMinutes(23));
            engine.Tick();
            Assert.Equal(TimerPhase.PreWarning, engine.Phase);
            Assert.True(engine.CanExtend);

            Assert.True(engine.TryExtendFocus());
            Assert.Equal(TimerPhase.Focus, engine.Phase);
            Assert.Equal(TimeSpan.FromMinutes(7), engine.Remaining);
            Assert.False(engine.CanExtend);

            Assert.False(engine.TryExtendFocus());
            Assert.Equal(TimeSpan.FromMinutes(7), engine.Remaining);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryEmergencyExitBreak_RequiresExact24CharPhraseAndHold_AndLogsAudit()
    {
        var (engine, time, _, audit, tempDir) = CreateHarness();
        try
        {
            engine.StartFocus(TimerPreset.Starter25);
            time.Advance(TimeSpan.FromMinutes(25));
            engine.Tick();
            Assert.Equal(TimerPhase.Break, engine.Phase);
            string phrase = engine.EmergencyExitPhrase!;

            Assert.False(engine.TryEmergencyExitBreak(phrase, holdCompleted: false));
            Assert.False(engine.TryEmergencyExitBreak("WRONG", holdCompleted: true));
            Assert.Equal(TimerPhase.Break, engine.Phase);
            Assert.Empty(audit.Entries);

            time.Advance(TimeSpan.FromMinutes(1));
            Assert.True(engine.TryEmergencyExitBreak(phrase, holdCompleted: true));
            Assert.Equal(TimerPhase.Idle, engine.Phase);
            Assert.Single(audit.Entries);
            Assert.Equal("25/5", audit.Entries[0].PresetName);
            Assert.Equal(TimeSpan.FromMinutes(4), audit.Entries[0].RemainingBreak);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void RestoreFromStore_ResumesActiveBreakAfterRestart_AndSettlesExpiredBreakToIdle()
    {
        var (engine1, time, store, audit, tempDir) = CreateHarness();
        try
        {
            engine1.StartFocus(TimerPreset.Starter25, "Open PDF");
            time.Advance(TimeSpan.FromMinutes(25));
            engine1.Tick();
            string phrase = engine1.EmergencyExitPhrase!;

            time.Advance(TimeSpan.FromMinutes(2));
            var engine2 = new FocusSessionEngine(time, store, audit);
            engine2.RestoreFromStore();

            Assert.Equal(TimerPhase.Break, engine2.Phase);
            Assert.Equal(TimeSpan.FromMinutes(3), engine2.Remaining);
            Assert.Equal(phrase, engine2.EmergencyExitPhrase);

            time.Advance(TimeSpan.FromMinutes(5));
            var engine3 = new FocusSessionEngine(time, store, audit);
            engine3.RestoreFromStore();

            Assert.Equal(TimerPhase.BreakCompleted, engine3.Phase);
            Assert.Equal(TimeSpan.Zero, engine3.Remaining);

            engine3.FinishCycle();
            Assert.Equal(TimerPhase.Idle, engine3.Phase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
