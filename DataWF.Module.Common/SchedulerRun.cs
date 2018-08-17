using DataWF.Common;
using DataWF.Data;
using System;
using System.Linq;
using System.Threading;

namespace DataWF.Module.Common
{
    public enum SchedulerType
    {
        Interval,
        DayTime,
        WeekDay,
    }

    public class SchedulerExecute : IDisposable
    {
        public static SchedulerExecute Default = new SchedulerExecute();
        private ManualResetEvent delayEvent = new ManualResetEvent(false);
        private ManualResetEvent stopEvent = new ManualResetEvent(true);
        private ManualResetEvent pauseEvent = new ManualResetEvent(true);
        private Scheduler item;
        private SchedulerList items = new SchedulerList();
        private bool running = false;
        private int timer = 0;
        private int startH = 8;
        private int stoptH = 21;

        public SchedulerExecute(int timer = 60000)
        {
            this.timer = timer;
            items.Table.LoadItems("", DBLoadParam.Synchronize | DBLoadParam.CheckDeleted | DBLoadParam.ReferenceRow).LastOrDefault();
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
                ThreadPool.QueueUserWorkItem(p =>
                {
                    stopEvent.Reset();
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
                        item.Execute();
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
            DateTime now = DateTime.Now;

            if (item.Type == SchedulerType.Interval)
            {
                return now >= item.DateExecute + item.Interval;
            }
            else if (item.Type == SchedulerType.DayTime)
            {
                return (now - item.DateExecute.Value).Days > item.Interval.Value.Days && now.TimeOfDay > (item.Interval - TimeSpan.FromDays(item.Interval.Value.Days));
            }
            else if (item.Type == SchedulerType.WeekDay)
            {
                return (now - item.DateExecute.Value).Days >= 7 && (int)now.DayOfWeek == item.Interval.Value.Days && now.TimeOfDay > (item.Interval - TimeSpan.FromDays(item.Interval.Value.Days));
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
