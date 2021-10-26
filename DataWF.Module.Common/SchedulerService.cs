using DataWF.Common;
using DataWF.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public enum SchedulerType
    {
        Interval,
        DayTime,
        WeekDay,
        Manual
    }

    public class SchedulerService : IDisposable
    {
        public static SchedulerService Instance;
        private readonly ManualResetEventSlim delayEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim stopEvent = new ManualResetEventSlim(true);
        private readonly ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
        private Scheduler item;
        private readonly SchedulerList items = new SchedulerList();
        private bool running = false;
        private readonly int timer = 0;
        private readonly int startH = 8;
        private readonly int stoptH = 21;

        public SchedulerService(int timer = 60000)
        {
            Instance = this;
            this.timer = timer;
            items.Table.LoadItems("", DBLoadParam.Synchronize | DBLoadParam.CheckDeleted);
        }

        public bool Running
        {
            get { return running; }
        }

        public Scheduler Current
        {
            get { return item; }
        }

        public void Start()
        {
            if (!running)
            {
                running = true;
                _ = Task.Run(() =>
                   {
                       stopEvent.Reset();
                       delayEvent.Reset();
                       while (running)
                       {
                           Execute();
                           delayEvent.Wait(timer);
                           pauseEvent.Wait();
                       }
                       stopEvent.Set();
                   });
            }
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
                delayEvent.Set();
                stopEvent.Wait(timer);
            }
        }

        public void Pause()
        {
            pauseEvent.Reset();
        }

        public void Resume()
        {
            pauseEvent.Set();
        }

        public async void Execute()
        {
            DateTime now = DateTime.Now;
            if (now.Hour > stoptH || now.Hour < startH)
                return;

            foreach (var item in items)
            {
                if (Check(item))
                {
                    this.item = item;
                    try
                    {
                        await item.Execute();
                    }
                    catch (Exception e)
                    {
                        Helper.OnException(e);
                    }
                }
            }
        }

        public bool Check(Scheduler item)
        {
            var now = DateTime.Now;
            var dateExecute = item.DateExecute ?? now;

            if (item.Type == SchedulerType.Interval)
            {
                return now >= dateExecute + item.Interval;
            }
            else if (item.Type == SchedulerType.DayTime)
            {
                return (now - dateExecute).Days > item.Interval.Value.Days && now.TimeOfDay > (item.Interval - TimeSpan.FromDays(item.Interval.Value.Days));
            }
            else if (item.Type == SchedulerType.WeekDay)
            {
                return (now - dateExecute).Days >= 7 && (int)now.DayOfWeek == item.Interval.Value.Days && now.TimeOfDay > (item.Interval - TimeSpan.FromDays(item.Interval.Value.Days));
            }
            return false;
        }

        public SchedulerList Items
        {
            get { return items; }
        }

        public void Dispose()
        {
            Stop();
            if (items != null)
                items.Dispose();
        }
    }
}
