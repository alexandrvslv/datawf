using DataWF.Common;
using DataWF.Data;
using DataWF.WebService.Common;
using DataWF.WebClient.Common;
using System.Collections.Generic;


[assembly: Invoker(typeof(PageContent<>), nameof(PageContent<DBItem>.Info), typeof(PageContent<>.InfoInvoker))]
[assembly: Invoker(typeof(PageContent<>), nameof(PageContent<DBItem>.Items), typeof(PageContent<>.ItemsInvoker))]
namespace DataWF.WebService.Common
{
    public class PageContent<T> where T : DBItem
    {
        public HttpPageSettings Info { get; set; }

        public IEnumerable<T> Items { get; set; }

        public class InfoInvoker : Invoker<PageContent<T>, HttpPageSettings>
        {
            public override string Name => nameof(Info);

            public override bool CanWrite => true;

            public override HttpPageSettings GetValue(PageContent<T> target) => target.Info;

            public override void SetValue(PageContent<T> target, HttpPageSettings value) => target.Info = value;
        }

        public class ItemsInvoker : Invoker<PageContent<T>, IEnumerable<T>>
        {
            public override string Name => nameof(Items);

            public override bool CanWrite => true;

            public override IEnumerable<T> GetValue(PageContent<T> target) => target.Items;

            public override void SetValue(PageContent<T> target, IEnumerable<T> value) => target.Items = value;
        }
    }
}
