using DataWF.Generator.Records;

namespace DataWF.Generator;

internal record struct CtorInfo(string Name, TypeRecord DeclareType, bool HasInstance, ConstructorRecord Constructor);


