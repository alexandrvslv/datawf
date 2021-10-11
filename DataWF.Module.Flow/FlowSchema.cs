using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Messanger;

namespace DataWF.Module.Flow
{
    [Schema("flow_schema")]
    [SchemaEntry(typeof(Template))]
    [SchemaEntry(typeof(TemplateData))]
    [SchemaEntry(typeof(TemplateFile))]
    [SchemaEntry(typeof(TemplateProperty))]
    [SchemaEntry(typeof(TemplateReference))]
    [SchemaEntry(typeof(Work))]
    [SchemaEntry(typeof(Stage))]
    [SchemaEntry(typeof(StageParam))]
    [SchemaEntry(typeof(StageProcedure))]
    [SchemaEntry(typeof(StageReference))]
    [SchemaEntry(typeof(StageTemplate))]
    [SchemaEntry(typeof(StageForeign))]
    [SchemaEntry(typeof(Document))]
    [SchemaEntry(typeof(DocumentComment))]
    [SchemaEntry(typeof(DocumentCustomer))]
    [SchemaEntry(typeof(DocumentData))]
    [SchemaEntry(typeof(DocumentReference))]
    [SchemaEntry(typeof(DocumentWork))]
    public partial class FlowSchema : MessageSchema
    {

    }
}
