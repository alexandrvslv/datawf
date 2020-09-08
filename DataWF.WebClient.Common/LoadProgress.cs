using DataWF.WebClient.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class LoadProgress<T>
    {
        public LoadProgress(string filter, HttpPageSettings list, HttpJsonSettings json, ProgressToken progressToken)
        {
            Filter = filter;
            ListSettings = list;
            JsonSettings = json;
            Token = progressToken;
        }

        public ProgressToken Token { get; }
        public Task<List<T>> Task { get; set; }
        public string Filter { get; internal set; }
        public HttpPageSettings ListSettings { get; }
        public HttpJsonSettings JsonSettings { get; }
    }
}