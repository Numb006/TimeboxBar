using System;
using System.Windows.Forms;

namespace TimeboxBar.Core
{
    public enum TimerState { Idle, Running, Paused }

    public class TimeboxTimer
    {
        private readonly Timer _tick;

        private DateTime  _startUtc;
        private TimeSpan  _pauseOffset;   // Summe aller vergangenen Pausenzeiten
        private DateTime  _pauseStartUtc; // Zeitpunkt des letzten Pause-Beginns
        private TimerState _state = TimerState.Idle;
        private TimeSpan  _duration;

        public event Action<TimeSpan> Tick;
        public event Action           Completed;
        public event Action<TimerState> StateChanged;

        public TimerState State    => _state;
        public TimeSpan   Duration => _duration;

        public TimeSpan Remaining
        {
            get
            {
                if (_state == TimerState.Idle) return TimeSpan.Zero;

                TimeSpan activeElapsed;
                if (_state == TimerState.Paused)
                    // Pause läuft gerade: aktive Zeit bis Pause-Beginn
                    activeElapsed = (_pauseStartUtc - _startUtc) - _pauseOffset;
                else
                    // Timer läuft: aktive Zeit bis jetzt
                    activeElapsed = (DateTime.UtcNow - _startUtc) - _pauseOffset;

                var rem = _duration - activeElapsed;
                return rem < TimeSpan.Zero ? TimeSpan.Zero : rem;
            }
        }

        public double Progress
        {
            get
            {
                if (_state == TimerState.Idle || _duration == TimeSpan.Zero) return 0;
                return Remaining.TotalSeconds / _duration.TotalSeconds;
            }
        }

        public TimeboxTimer()
        {
            _tick = new Timer { Interval = 200 };
            _tick.Tick += OnTick;
        }

        public void Start(TimeSpan duration)
        {
            _duration     = duration;
            _startUtc     = DateTime.UtcNow;
            _pauseOffset  = TimeSpan.Zero;
            SetState(TimerState.Running);
            _tick.Start();
        }

        public void Pause()
        {
            if (_state != TimerState.Running) return;
            _pauseStartUtc = DateTime.UtcNow;
            _tick.Stop();
            SetState(TimerState.Paused);
        }

        public void Resume()
        {
            if (_state != TimerState.Paused) return;
            // Pausendauer zur Offset-Summe addieren, damit sie nicht als aktive Zeit gilt
            _pauseOffset += DateTime.UtcNow - _pauseStartUtc;
            SetState(TimerState.Running);
            _tick.Start();
        }

        public void TogglePause()
        {
            if (_state == TimerState.Running) Pause();
            else if (_state == TimerState.Paused) Resume();
        }

        public void Stop()
        {
            _tick.Stop();
            SetState(TimerState.Idle);
        }

        private void OnTick(object sender, EventArgs e)
        {
            var rem = Remaining;
            Tick?.Invoke(rem);

            if (rem == TimeSpan.Zero)
            {
                _tick.Stop();
                SetState(TimerState.Idle);
                Completed?.Invoke();
            }
        }

        private void SetState(TimerState s)
        {
            _state = s;
            StateChanged?.Invoke(s);
        }
    }
}
