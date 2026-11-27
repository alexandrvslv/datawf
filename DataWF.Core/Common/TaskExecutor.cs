using System;
using System.Threading;

namespace DataWF;

public class TaskExecutor
{
    private string name;
    private Thread thread;
    public ThreadStart Start;

    public TaskAction Action { get; set; }
    public object Object { get; set; }
    public object Tag { get; set; }
    public TimeSpan Time { get; set; }

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public bool IsAsynchStart => thread != null;

    public bool IsAsynchComplete
    {
        get
        {
            if (thread != null)
                return thread.ThreadState == ThreadState.Stopped;
            return false;
        }
    }

    public event Action<RProcedureEventArgs> Callback;

    public object Execute()
    {
        object result = null;
        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            result = Action();
            watch.Stop();
            Time = watch.Elapsed;
            OnCallback(result);
        }
        catch (Exception e)
        {
            OnCallback(e);
        }
        return result;

    }

    public void ExecuteAsynch()
    {
        if (thread == null)
        {
            if (Start == null)
                Start = new ThreadStart(() => Execute());
            thread = new Thread(Start);
        }
        thread.Start();
    }

    public void Cancel()
    {
        if (thread != null && !name.StartsWith("Cancelation!", StringComparison.OrdinalIgnoreCase))
        {
            name = "Cancelation! " + Name;
            thread.Abort();
        }
    }

    public void OnCallback(object result)
    {
        Callback?.Invoke(new RProcedureEventArgs { Task = this, Result = result });
    }
}

public delegate object TaskAction();
