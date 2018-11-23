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
    }

    public class SchedulerService : IDisposable
    {
        public static SchedulerService Instance;
        private ManualResetEvent delayEvent = new ManualResetEvent(false);
        private ManualResetEvent stopEvent = new ManualResetEvent(true);
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);
        private Scheduler item;
        private SchedulerList items = new SchedulerList();
        private bool running = false;
        private readonly int timer = 0;
        private readonly int startH = 8;
        private readonly int stoptH = 21;

        public SchedulerService(int timer = 60000)
        {
            Instance = this;
            this.timer = timer;
            items.Table.LoadItems("", DBLoadParam.Synchronize | DBLoadParam.CheckDeleted | DBLoadParam.Reference).LastOrDefault();
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
                           delayEvent.WaitOne(timer);
                           pauseEvent.WaitOne();
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
                stopEvent.WaitOne(timer);
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

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (now.Hour > stoptH || now.Hour < startH)
                return;
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    item = items[i];
                    if (Check(item))
                    {
                        item.Execute(null);
                    }
                }
                item = null;
            }
            catch (Exception e)
            {
                Helper.OnException(e);
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
