using DataWF.Invokers;

namespace DataWF.Models;

public interface IModelItem
{
    string Name { get; set; }
    IModelSchema? Schema { get; }
}