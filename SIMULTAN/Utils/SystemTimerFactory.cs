using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SIMULTAN.Utils
{
    public class SystemTimerFactory : IDispatcherTimerFactory
    {
        public IDispatcherTimer Create()
        {
            return new SystemTimer();
        }

        internal class SystemTimer : IDispatcherTimer
        {
            public TimeSpan Interval
            {
                get
                {
                    return TimeSpan.FromMilliseconds(timer.Interval);
                }
                set
                {
                    timer.Interval = value.TotalMilliseconds;
                }
            }

            private event EventHandler Tick;
            private Timer timer;

            public SystemTimer()
            {
                timer = new Timer();
                timer.Elapsed += this.Timer_Elapsed;
            }

            private void Timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                Tick?.Invoke(this, e);
            }

            public void Start()
            {
                timer.Start();
            }

            public void Stop()
            {
                timer.Stop();
            }

            public void AddTickEventHandler(EventHandler handler)
            {
                Tick += handler;
            }

            public void RemoveTickEventHandler(EventHandler handler)
            {
                Tick -= handler;
            }
        }
    }
}
