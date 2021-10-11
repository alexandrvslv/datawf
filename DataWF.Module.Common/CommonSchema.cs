using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Common
{
    [Schema("common_schema")]
    [SchemaEntry(typeof(FileData))]
    [SchemaEntry(typeof(Instance))]
    [SchemaEntry(typeof(Book))]
    [SchemaEntry(typeof(Department))]
    [SchemaEntry(typeof(Position))]
    [SchemaEntry(typeof(UserGroup))]
    [SchemaEntry(typeof(GroupPermission))]
    [SchemaEntry(typeof(User))]
    [SchemaEntry(typeof(UserReg))]
    [SchemaEntry(typeof(UserFile))]
    [SchemaEntry(typeof(UserApplication))]    
    [SchemaEntry(typeof(Scheduler))]
    public partial class CommonSchema : CounterpartSchema
    {
    }
}
