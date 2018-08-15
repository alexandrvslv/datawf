using System;
using System.ComponentModel;
using System.Threading;
using DataWF.Common;
using DataWF.Data;
using System.Collections.Concurrent;
using Xwt;
using System.Threading.Tasks;

namespace DataWF.Data.Gui
{
    public class TableLoader : IDisposable
    {
        private ManualResetEvent cancelEvent = new ManualResetEvent(true);
        private ManualResetEvent delayEvent = new ManualResetEvent(false);
        private ConcurrentStack<QQuery> queries = new ConcurrentStack<QQuery>();
        private DateTime changeProgressSpeed = DateTime.UtcNow;
        private IDBTableView view;
        private DBTransaction transaction = null;
        // private EventHandler<DBLoadCompleteEventArgs> _handlerComplete = null;
        private EventHandler<DBLoadProgressEventArgs> handlerProgress = null;
        private EventHandler<DBLoadColumnsEventArgs> handlerColumns = null;

        public TableLoader()
            : base()
        {
            //_handlerComplete = DBTableLoadComplete;
            handlerProgress = DBTableLoadProgress;
            handlerColumns = DBTableLoadColumns;
        }

        public event EventHandler<EventArgs> LoadComplete;

        public event EventHandler<DBLoadProgressEventArgs> LoadProgress;

        public event EventHandler<DBLoadColumnsEventArgs> LoadColumns;

        public void Dispose()
        {
            if (view != null)
            {
                //view.Table.LoadComplete -= _handlerComplete;
                view.Table.LoadProgress -= handlerProgress;
                view.Table.LoadColumns -= handlerColumns;
                Cancel();
            }
        }

        public IDBTableView View
        {
            get { return view; }
            set
            {
                if (view == value)
                    return;
                if (view != null)
                {
                    //view.Table.LoadComplete -= _handlerComplete;
                    view.Table.LoadProgress -= handlerProgress;
                    view.Table.LoadColumns -= handlerColumns;
                    Cancel();
                }

                view = value;
                if (view != null)
                {
                    //view.Table.LoadComplete += _handlerComplete;
                    view.Table.LoadProgress += handlerProgress;
                    view.Table.LoadColumns += handlerColumns;
                }
            }
        }

        private void OnLoadProgress(object val)
        {
            LoadProgress?.Invoke(this, (DBLoadProgressEventArgs)val);
        }

        private void DBTableLoadProgress(object sender, DBLoadProgressEventArgs arg)
        {
            if (arg.Target == view)
            {
                TimeSpan ts = DateTime.UtcNow - changeProgressSpeed;
                if (ts.TotalMilliseconds > 300)
                {
                    changeProgressSpeed = DateTime.UtcNow;
                    Application.Invoke(() => OnLoadProgress(arg));
                }
            }
        }

        private void OnLoadComplete(object val)
        {
            LoadComplete?.Invoke(this, (EventArgs)val);
        }

        private void DBTableLoadComplete(object sender, EventArgs arg)
        {
            Application.Invoke(() => OnLoadComplete(arg));
        }

        private void OnLoadColumns(object val)
        {
            LoadColumns?.Invoke(this, (DBLoadColumnsEventArgs)val);
        }

        private void DBTableLoadColumns(object sender, DBLoadColumnsEventArgs arg)
        {
            if (arg.Target == view)
                Application.Invoke(() => OnLoadColumns(arg));
        }

        public async void LoadAsync(QQuery query)
        {
            Cancel();
            if (query != null && query.Table == view.Table)
            {
                queries.Push(query);
                await Task.Run(() => Loader()).ConfigureAwait(false);
            }
        }

        public void LoadAsync()
        {
            if (view == null || view.Table.IsSynchronized)
                return;
            LoadAsync(view.Query);
        }

        private void Loader()
        {
            delayEvent.WaitOne(500);
            QQuery query;
            if (queries.TryPop(out query))
            {
                cancelEvent.WaitOne();
                try
                {
                    DBTableLoadProgress(view.Table, new DBLoadProgressEventArgs(view, 0, 0, null));
                    using (var temp = new DBTransaction(view.Table.Schema.Connection) { View = view })//DBLoadParam.GetCount | DBLoadParam.ReferenceRow
                    {
                        transaction = temp;
                        View.Table.LoadItems(query);
                    }
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
                finally
                {
                    transaction = null;
                    cancelEvent.Set();
                    DBTableLoadComplete(this, EventArgs.Empty);
                }
            }
        }

        public bool IsLoad()
        {
            return transaction != null;// && !transaction.Canceled;
        }

        public void Cancel()
        {
            queries.Clear();
            if (IsLoad())
            {
                transaction.Cancel();
                cancelEvent.Reset();
            }
        }
    }

    public interface ISync
    {
        void Sync();
        Task SyncAsync();
    }

    public interface ILoader
    {
        TableLoader Loader { get; }
    }
}
