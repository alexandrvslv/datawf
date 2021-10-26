using System;

namespace DataWF.Common
{
	public class StateInfoEventArgs : EventArgs
	{
		public StateInfoEventArgs(StateInfo log)
		{
			Log = log;
		}

		public StateInfo Log { get; private set; }
	}
}

