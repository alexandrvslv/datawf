using System;

namespace DataWF
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

