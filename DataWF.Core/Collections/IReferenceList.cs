using System.Collections.Generic;

namespace DataWF.Collections;

public interface IReferenceList : IChangeableList
{
    ModelEntry Owner { get; set; }
    string OwnerProperty { get; set; }
}
