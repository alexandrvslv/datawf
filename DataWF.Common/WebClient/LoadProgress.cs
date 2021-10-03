using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class LoadProgress<T> : ILoadProgress
    {
        public LoadProgress(string filter, ProgressToken progressToken)
        {
            Filter = filter;
            Token = progressToken;
        }

        public ProgressToken Token { get; }
        public Task<List<T>> Task { get; set; }
        public string Filter { get; internal set; }

        public async Task<IList> RunTask() => await Task;
        
    }
}