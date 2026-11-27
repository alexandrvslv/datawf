using System;

namespace DataWF
{
    public class RProcedureEventArgs : EventArgs
    {
        public TaskExecutor Task { get; set; }
        public object Result { get; set; }
    }

}
