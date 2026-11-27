using DataWF.Invokers;

namespace DataWF.Serialization;

public interface ISerializerRegister : IInvokerRegister
{
    static virtual IElementSerializer RegisterSerializer() => null!;
}
