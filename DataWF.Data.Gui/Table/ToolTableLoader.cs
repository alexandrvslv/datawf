using System;
using DataWF.Data;
using DataWF.Gui;
using Xwt;

namespace DataWF.Data.Gui
{
    public class ToolTableLoader : ToolProgressBar
    {
        private TableLoader loader;

        public TableLoader Loader
        {
            get { return loader; }
            set
            {
                if (loader != value)
                {
                    if (loader != null)
                    {
                        loader.LoadProgress -= OnProgess;
                        loader.LoadComplete -= OnComplete;
                    }
                    loader = value;
                    if (loader != null)
                    {
                        loader.LoadProgress += OnProgess;
                        loader.LoadComplete += OnComplete;
                    }
                }
            }
        }

        protected void OnProgess(object sender, DBLoadProgressEventArgs arg)
        {
            Visible = true;
            Value = arg.Percentage;
            ProgressBar.TooltipText = arg.Current + "/" + arg.TotalCount;
        }

        protected void OnComplete(object sender, EventArgs arg)
        {
            Visible = false;
            ProgressBar.Fraction = 0;
            ProgressBar.TooltipText = "/";
        }

        public override void Dispose()
        {
            Loader = null;
            base.Dispose();
        }
    }
}
