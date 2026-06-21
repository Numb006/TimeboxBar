using System;
using System.Threading;
using NUnit.Framework;
using TimeboxBar.Core;

namespace TimeboxBar.Tests
{
    [TestFixture]
    public class TimeboxTimerTests
    {
        private TimeboxTimer _timer;

        [SetUp]
        public void SetUp()
        {
            _timer = new TimeboxTimer();
        }

        [TearDown]
        public void TearDown()
        {
            _timer.Stop();
        }

        [Test]
        public void InitialState_IsIdle()
        {
            Assert.That(_timer.State, Is.EqualTo(TimerState.Idle));
        }

        [Test]
        public void InitialRemaining_IsZero()
        {
            Assert.That(_timer.Remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Start_SetsRunningState()
        {
            _timer.Start(TimeSpan.FromMinutes(25));
            Assert.That(_timer.State, Is.EqualTo(TimerState.Running));
        }

        [Test]
        public void Start_RemainingCloseToFullDuration()
        {
            var duration = TimeSpan.FromMinutes(25);
            _timer.Start(duration);
            var remaining = _timer.Remaining;
            // Kurz nach Start: Remaining sollte sehr nah an der Dauer liegen
            Assert.That(remaining.TotalSeconds, Is.GreaterThan(24 * 60));
            Assert.That(remaining.TotalSeconds, Is.LessThanOrEqualTo(25 * 60));
        }

        [Test]
        public void Start_ProgressIsOne_Immediately()
        {
            _timer.Start(TimeSpan.FromMinutes(10));
            Assert.That(_timer.Progress, Is.EqualTo(1.0).Within(0.01));
        }

        [Test]
        public void Pause_SetsPausedState()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.Pause();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Paused));
        }

        [Test]
        public void Pause_FreezesRemaining()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            Thread.Sleep(100);
            _timer.Pause();
            var rem1 = _timer.Remaining;
            Thread.Sleep(200);
            var rem2 = _timer.Remaining;
            Assert.That(rem1, Is.EqualTo(rem2),
                "Remaining sollte während der Pause eingefroren sein");
        }

        [Test]
        public void Pause_OnIdleTimer_DoesNothing()
        {
            _timer.Pause(); // Idle → kein State-Change
            Assert.That(_timer.State, Is.EqualTo(TimerState.Idle));
        }

        [Test]
        public void Resume_SetsRunningState()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.Pause();
            _timer.Resume();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Running));
        }

        [Test]
        public void Resume_PauseTimeNotCountedAsActiveTime()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            Thread.Sleep(50);
            _timer.Pause();
            var remAtPause = _timer.Remaining;
            Thread.Sleep(300); // 300ms Pausenzeit — soll nicht zählen
            _timer.Resume();
            var remAfterResume = _timer.Remaining;
            // Remaining nach Resume sollte sehr ähnlich zu Remaining bei Pause sein (Toleranz 100ms)
            Assert.That(Math.Abs((remAtPause - remAfterResume).TotalMilliseconds),
                Is.LessThan(100),
                "Pausenzeit soll nicht in aktiver Zeit mitzählen");
        }

        [Test]
        public void MultiplePauseResume_NoAccumulatedDrift()
        {
            _timer.Start(TimeSpan.FromMinutes(10));
            Thread.Sleep(30);
            for (int i = 0; i < 5; i++)
            {
                _timer.Pause();
                Thread.Sleep(100); // 100ms Pause, soll nicht zählen
                _timer.Resume();
                Thread.Sleep(10);  // 10ms aktiv
            }
            // Nach 5x 100ms Pause + 5x 10ms aktiv: ~50ms aktive Zeit vergangen
            // Remaining sollte nahe 10 Minuten sein
            Assert.That(_timer.Remaining.TotalSeconds, Is.GreaterThan(9 * 60 + 50));
        }

        [Test]
        public void Stop_SetsIdleState()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.Stop();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Idle));
        }

        [Test]
        public void Stop_RemainingIsZero()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.Stop();
            Assert.That(_timer.Remaining, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void TogglePause_FromRunning_PausesTimer()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.TogglePause();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Paused));
        }

        [Test]
        public void TogglePause_FromPaused_ResumesTimer()
        {
            _timer.Start(TimeSpan.FromMinutes(5));
            _timer.Pause();
            _timer.TogglePause();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Running));
        }

        [Test]
        public void TogglePause_FromIdle_DoesNothing()
        {
            _timer.TogglePause();
            Assert.That(_timer.State, Is.EqualTo(TimerState.Idle));
        }

        [Test]
        public void StateChanged_FiredOnStart()
        {
            TimerState? fired = null;
            _timer.StateChanged += s => fired = s;
            _timer.Start(TimeSpan.FromMinutes(1));
            Assert.That(fired, Is.EqualTo(TimerState.Running));
        }

        [Test]
        public void StateChanged_FiredOnStop()
        {
            _timer.Start(TimeSpan.FromMinutes(1));
            TimerState? fired = null;
            _timer.StateChanged += s => fired = s;
            _timer.Stop();
            Assert.That(fired, Is.EqualTo(TimerState.Idle));
        }

        [Test]
        public void Progress_IsZero_WhenIdle()
        {
            Assert.That(_timer.Progress, Is.EqualTo(0.0));
        }

        [Test]
        public void Duration_ReflectsStartedDuration()
        {
            var dur = TimeSpan.FromMinutes(42);
            _timer.Start(dur);
            Assert.That(_timer.Duration, Is.EqualTo(dur));
        }
    }
}
