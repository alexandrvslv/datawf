using System;

namespace DataWF.Collections.Query;

[Flags]
public enum CompareOperatorType : int
{
    Undefined = 0,
    Not = 1,
    Equal = 2,
    In = 4,
    Like = 8,
    Between = 16,
    Greater = 32,
    Less = 64,
    Is = 128,
    Null = 256,
    True = 512,
    False = 1024,
    And = 2048,
    AndAlso = 4096,
    Or = 8192,
    OrElse = 16384,
    AndNot = AndAlso | Not,
    OrNot = OrElse | Not,
    GreaterOrEqual = Greater | Equal,
    LessOrEqual = Less | Equal,
    IsNull = Is | Null,
    IsNotNull = Is | Not | Null,
    IsTrue = Is | True,
    IsNotTrue = Is | Not | True,
    IsFalse = Is | False,
    IsNotFalse = Is | Not | False,
    NotEqual = Not | Equal,
    NotIn = Not | In,
    NotLike = Not | Like,
    NotBetween = Not | Between
}

