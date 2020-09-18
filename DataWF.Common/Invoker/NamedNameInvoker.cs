using DataWF.Common;

[assembly: Invoker(typeof(INamed), nameof(INamed.Name), typeof(NamedNameInvoker<>))]
namespace DataWF.Common
{

    public class NamedNameInvoker<T> : Invoker<T, string> where T : INamed
    {
        public static readonly NamedNameInvoker<T> Instance = new NamedNameInvoker<T>();

        public override string Name
        {
            get => nameof(INamed.Name);
        }

        public override bool CanWrite => true;

        public override string GetValue(T target) => target.Name;

        public override void SetValue(T target, string value) => target.Name = value;
    }


}
