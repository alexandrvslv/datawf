using DataWF.Data;
using System;

namespace DataWF.Module.Common
{
    public partial class SchedulerTable
    {
        SchedulerService Instance { get; set; }

        [ControllerMethod]
        public void Start()
        {
            if (Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            if (!Instance.Running)
            {
                Instance.Start();
            }
        }

        [ControllerMethod]
        public void Stop()
        {
            if (Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            if (Instance.Running)
            {
                Instance.Stop();
            }
        }

        [ControllerMethod]
        public bool IsRunning()
        {
            if (Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            return Instance.Running;
        }
    }
}
