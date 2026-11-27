//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Collections.Query.Evaluators;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DataWF.Collections.Query;

public readonly struct MathOperator
{
    public static readonly MathOperator MathPlusOperator = new(MathOperatorType.Plus, new MathDecimalPlusEvaluator());
    public static readonly MathOperator MathMinusOperator = new(MathOperatorType.Minus, new MathDecimalMinusEvaluator());
    public static readonly MathOperator MathNegationOperator = new(MathOperatorType.Minus, new MathDecimalNegationEvaluator());
    public static readonly MathOperator MathMultiplyOperator = new(MathOperatorType.Multiply, new MathDecimalMultiplyEvaluator());
    public static readonly MathOperator MathDevideOperator = new(MathOperatorType.Divide, new MathDecimalDivideEvaluator());
    public static readonly MathOperator MathModuloOperator = new(MathOperatorType.Modulo, new MathDecimalModuloEvaluator());
    public static readonly MathOperator MathExponentaOperator = new(MathOperatorType.Exponenta, new MathDecimalExponentaEvaluator());
    public static readonly MathOperator MathAbsOperator = new(MathOperatorType.Abs, new MathDecimalAbsEvaluator());
    public static readonly MathOperator MathBitwiseAndOperator = new(MathOperatorType.BitwiseAnd, new MathDecimalBitwiseAndEvaluator());
    public static readonly MathOperator MathBitwiseOrOperator = new(MathOperatorType.BitwiseOr, new MathDecimalBitwiseOrEvaluator());
    public static readonly MathOperator MathBitwiseXorOperator = new(MathOperatorType.BitwiseXor, new MathDecimalBitwiseXorEvaluator());
    public static readonly MathOperator MathBitwiseNotOperator = new(MathOperatorType.BitwiseNot, new MathDecimalBitwiseNotEvaluator());

    public static implicit operator MathOperator(MathOperatorType type) => Fabric(type);
    public static MathOperator Fabric(char symbol) => Fabric(Parse(symbol));
    public static MathOperator Fabric(MathOperatorType type) => type switch
    {
        MathOperatorType.Plus => MathPlusOperator,
        MathOperatorType.Minus => MathMinusOperator,
        MathOperatorType.Multiply => MathMultiplyOperator,
        MathOperatorType.Divide => MathDevideOperator,
        MathOperatorType.Modulo => MathDevideOperator,
        MathOperatorType.Exponenta => MathExponentaOperator,
        MathOperatorType.Abs => MathExponentaOperator,
        MathOperatorType.BitwiseAnd => MathBitwiseAndOperator,
        MathOperatorType.BitwiseOr => MathBitwiseOrOperator,
        MathOperatorType.BitwiseXor => MathBitwiseXorOperator,
        MathOperatorType.BitwiseNot => MathBitwiseNotOperator,
        _ => throw new NotSupportedException(),
    };

    public MathOperator(MathOperatorType mathType, IEvaluator evaluator)
    {
        Type = mathType;
        Evaluator = evaluator;
    }

    public MathOperatorType Type { get; }
    public IEvaluator Evaluator { get; }
    public int ParametersCount => Evaluator.ParametersCount;

    public static MathOperatorType Parse(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => MathOperatorType.Plus,
            ExpressionType.AddChecked => MathOperatorType.Plus,
            ExpressionType.Increment => MathOperatorType.Plus,
            ExpressionType.Subtract => MathOperatorType.Minus,
            ExpressionType.Decrement => MathOperatorType.Minus,
            ExpressionType.Divide => MathOperatorType.Divide,
            ExpressionType.Multiply => MathOperatorType.Multiply,
            ExpressionType.MultiplyChecked => MathOperatorType.Multiply,
            ExpressionType.Modulo => MathOperatorType.Modulo,
            ExpressionType.And => MathOperatorType.BitwiseAnd,
            ExpressionType.Or => MathOperatorType.BitwiseOr,
            ExpressionType.Not => MathOperatorType.BitwiseNot,
            ExpressionType.ExclusiveOr => MathOperatorType.BitwiseXor,
            _ => MathOperatorType.Undefined,
        };
    }

    public static MathOperatorType Parse(char code)
    {
        MathOperatorType en = MathOperatorType.Undefined;
        switch (code)
        {
            case '+': en = MathOperatorType.Plus; break;
            case '-': en = MathOperatorType.Minus; break;
            case '/': en = MathOperatorType.Divide; break;
            case '*': en = MathOperatorType.Multiply; break;
            case '%': en = MathOperatorType.Modulo; break;
            case '^': en = MathOperatorType.Exponenta; break;
            case '@': en = MathOperatorType.Abs; break;
            case '&': en = MathOperatorType.BitwiseAnd; break;
            case '|': en = MathOperatorType.BitwiseOr; break;
            case '#': en = MathOperatorType.BitwiseXor; break;
            case '~': en = MathOperatorType.BitwiseNot; break;
        }

        return en;
    }

    public static string Format(MathOperatorType type)
    {
        switch (type)
        {
            case MathOperatorType.Divide: return "/";
            case MathOperatorType.Minus: return "-";
            case MathOperatorType.Multiply: return "*";
            case MathOperatorType.Plus: return "+";
            case MathOperatorType.Modulo: return "%";
            case MathOperatorType.Exponenta: return "^";
            case MathOperatorType.BitwiseAnd: return "&";
            case MathOperatorType.BitwiseOr: return "|";
            case MathOperatorType.BitwiseXor: return "#";
            case MathOperatorType.BitwiseNot: return "~";
        }
        return "";
    }

    public IMathUnaryEvaluator<V> GetUnaryEvaluator<V>()
    {
        if (UnaryEvaluators.TryGetValue(Type, out var types)
            && types.TryGetValue(typeof(V), out var evaluator))
            return (IMathUnaryEvaluator<V>)evaluator;
        throw new NotImplementedException($"GetUnaryEvaluator Operator:{Type} Type:{typeof(V)}");
    }

    public IMathBinaryEvaluator<V> GetBinaryEvaluator<V>()
    {
        if (BinaryEvaluators.TryGetValue(Type, out var types)
            && types.TryGetValue(typeof(V), out var evaluator))
            return (IMathBinaryEvaluator<V>)evaluator;
        throw new NotImplementedException($"GetBinaryEvaluator Operator:{Type} Type:{typeof(V)}");
    }

    public static readonly Dictionary<MathOperatorType, Dictionary<Type, IEvaluator>> UnaryEvaluators = new()
        {
            { MathOperatorType.Minus, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalNegationEvaluator() },
                    {typeof(double), new MathDoubleNegationEvaluator() },
                    {typeof(float), new MathFloatNegationEvaluator() },
                    {typeof(long), new MathInt64NegationEvaluator() },
                    {typeof(int), new MathInt32NegationEvaluator() },
                    {typeof(short), new MathInt16NegationEvaluator() },
                    {typeof(sbyte), new MathInt8NegationEvaluator() },
                    {typeof(decimal?), new MathNDecimalNegationEvaluator() },
                    {typeof(double?), new MathNDoubleNegationEvaluator() },
                    {typeof(float?), new MathNFloatNegationEvaluator() },
                    {typeof(long?), new MathNInt64NegationEvaluator() },
                    {typeof(int?), new MathNInt32NegationEvaluator() },
                    {typeof(short?), new MathNInt16NegationEvaluator() },
                    {typeof(sbyte?), new MathNInt8NegationEvaluator() },
                }
            },
            { MathOperatorType.Abs, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalAbsEvaluator() },
                    {typeof(double), new MathDoubleAbsEvaluator() },
                    {typeof(float), new MathFloatAbsEvaluator() },
                    {typeof(long), new MathInt64AbsEvaluator() },
                    {typeof(int), new MathInt32AbsEvaluator() },
                    {typeof(short), new MathInt16AbsEvaluator() },
                    {typeof(sbyte), new MathInt8AbsEvaluator() },
                    {typeof(decimal?), new MathNDecimalAbsEvaluator() },
                    {typeof(double?), new MathNDoubleAbsEvaluator() },
                    {typeof(float?), new MathNFloatAbsEvaluator() },
                    {typeof(long?), new MathNInt64AbsEvaluator() },
                    {typeof(int?), new MathNInt32AbsEvaluator() },
                    {typeof(short?), new MathNInt16AbsEvaluator() },
                    {typeof(sbyte?), new MathNInt8AbsEvaluator() },
                }
            },
            { MathOperatorType.BitwiseNot, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalBitwiseNotEvaluator() },
                    {typeof(double), new MathDoubleBitwiseNotEvaluator() },
                    {typeof(float), new MathFloatBitwiseNotEvaluator() },
                    {typeof(long), new MathInt64BitwiseNotEvaluator() },
                    {typeof(int), new MathInt32BitwiseNotEvaluator() },
                    {typeof(short), new MathInt16BitwiseNotEvaluator() },
                    {typeof(sbyte), new MathInt8BitwiseNotEvaluator() },
                    {typeof(decimal?), new MathNDecimalBitwiseNotEvaluator() },
                    {typeof(double?), new MathNDoubleBitwiseNotEvaluator() },
                    {typeof(float?), new MathNFloatBitwiseNotEvaluator() },
                    {typeof(long?), new MathNInt64BitwiseNotEvaluator() },
                    {typeof(int?), new MathNInt32BitwiseNotEvaluator() },
                    {typeof(short?), new MathNInt16BitwiseNotEvaluator() },
                    {typeof(sbyte?), new MathNInt8BitwiseNotEvaluator() },
                }
            }
        };

    public static readonly Dictionary<MathOperatorType, Dictionary<Type, IEvaluator>> BinaryEvaluators = new()
        {
            { MathOperatorType.Plus, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalPlusEvaluator() },
                    {typeof(double), new MathDoublePlusEvaluator() },
                    {typeof(float), new MathFloatPlusEvaluator() },
                    {typeof(long), new MathInt64PlusEvaluator() },
                    {typeof(int), new MathInt32PlusEvaluator() },
                    {typeof(short), new MathInt16PlusEvaluator() },
                    {typeof(sbyte), new MathInt8PlusEvaluator() },
                    {typeof(decimal?), new MathNDecimalPlusEvaluator() },
                    {typeof(double?), new MathNDoublePlusEvaluator() },
                    {typeof(float?), new MathNFloatPlusEvaluator() },
                    {typeof(long?), new MathNInt64PlusEvaluator() },
                    {typeof(int?), new MathNInt32PlusEvaluator() },
                    {typeof(short?), new MathNInt16PlusEvaluator() },
                    {typeof(sbyte?), new MathNInt8PlusEvaluator() },
                }
            },
            { MathOperatorType.Minus, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalMinusEvaluator() },
                    {typeof(double), new MathDoubleMinusEvaluator() },
                    {typeof(float), new MathFloatMinusEvaluator() },
                    {typeof(long), new MathInt64MinusEvaluator() },
                    {typeof(int), new MathInt32MinusEvaluator() },
                    {typeof(short), new MathInt16MinusEvaluator() },
                    {typeof(sbyte), new MathInt8MinusEvaluator() },
                    {typeof(decimal?), new MathNDecimalMinusEvaluator() },
                    {typeof(double?), new MathNDoubleMinusEvaluator() },
                    {typeof(float?), new MathNFloatMinusEvaluator() },
                    {typeof(long?), new MathNInt64MinusEvaluator() },
                    {typeof(int?), new MathNInt32MinusEvaluator() },
                    {typeof(short?), new MathNInt16MinusEvaluator() },
                    {typeof(sbyte?), new MathNInt8MinusEvaluator() },
                }
            },
            { MathOperatorType.Multiply, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalMultiplyEvaluator() },
                    {typeof(double), new MathDoubleMultiplyEvaluator() },
                    {typeof(float), new MathFloatMultiplyEvaluator() },
                    {typeof(long), new MathInt64MultiplyEvaluator() },
                    {typeof(int), new MathInt32MultiplyEvaluator() },
                    {typeof(short), new MathInt16MultiplyEvaluator() },
                    {typeof(sbyte), new MathInt8MultiplyEvaluator() },
                    {typeof(decimal?), new MathNDecimalMultiplyEvaluator() },
                    {typeof(double?), new MathNDoubleMultiplyEvaluator() },
                    {typeof(float?), new MathNFloatMultiplyEvaluator() },
                    {typeof(long?), new MathNInt64MultiplyEvaluator() },
                    {typeof(int?), new MathNInt32MultiplyEvaluator() },
                    {typeof(short?), new MathNInt16MultiplyEvaluator() },
                    {typeof(sbyte?), new MathNInt8MultiplyEvaluator() },
                }
            },
            { MathOperatorType.Divide, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalDivideEvaluator() },
                    {typeof(double), new MathDoubleDivideEvaluator() },
                    {typeof(float), new MathFloatDivideEvaluator() },
                    {typeof(long), new MathInt64DivideEvaluator() },
                    {typeof(int), new MathInt32DivideEvaluator() },
                    {typeof(short), new MathInt16DivideEvaluator() },
                    {typeof(sbyte), new MathInt8DivideEvaluator() },
                    {typeof(decimal?), new MathNDecimalDivideEvaluator() },
                    {typeof(double?), new MathNDoubleDivideEvaluator() },
                    {typeof(float?), new MathNFloatDivideEvaluator() },
                    {typeof(long?), new MathNInt64DivideEvaluator() },
                    {typeof(int?), new MathNInt32DivideEvaluator() },
                    {typeof(short?), new MathNInt16DivideEvaluator() },
                    {typeof(sbyte?), new MathNInt8DivideEvaluator() },
                }
            },
            { MathOperatorType.Modulo, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalModuloEvaluator() },
                    {typeof(double), new MathDoubleModuloEvaluator() },
                    {typeof(float), new MathFloatModuloEvaluator() },
                    {typeof(long), new MathInt64ModuloEvaluator() },
                    {typeof(int), new MathInt32ModuloEvaluator() },
                    {typeof(short), new MathInt16ModuloEvaluator() },
                    {typeof(sbyte), new MathInt8ModuloEvaluator() },
                    {typeof(decimal?), new MathNDecimalModuloEvaluator() },
                    {typeof(double?), new MathNDoubleModuloEvaluator() },
                    {typeof(float?), new MathNFloatModuloEvaluator() },
                    {typeof(long?), new MathNInt64ModuloEvaluator() },
                    {typeof(int?), new MathNInt32ModuloEvaluator() },
                    {typeof(short?), new MathNInt16ModuloEvaluator() },
                    {typeof(sbyte?), new MathNInt8ModuloEvaluator() },
                }
            },
            { MathOperatorType.BitwiseAnd, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalBitwiseAndEvaluator() },
                    {typeof(double), new MathDoubleBitwiseAndEvaluator() },
                    {typeof(float), new MathFloatBitwiseAndEvaluator() },
                    {typeof(long), new MathInt64BitwiseAndEvaluator() },
                    {typeof(int), new MathInt32BitwiseAndEvaluator() },
                    {typeof(short), new MathInt16BitwiseAndEvaluator() },
                    {typeof(sbyte), new MathInt8BitwiseAndEvaluator() },
                    {typeof(decimal?), new MathNDecimalBitwiseAndEvaluator() },
                    {typeof(double?), new MathNDoubleBitwiseAndEvaluator() },
                    {typeof(float?), new MathNFloatBitwiseAndEvaluator() },
                    {typeof(long?), new MathNInt64BitwiseAndEvaluator() },
                    {typeof(int?), new MathNInt32BitwiseAndEvaluator() },
                    {typeof(short?), new MathNInt16BitwiseAndEvaluator() },
                    {typeof(sbyte?), new MathNInt8BitwiseAndEvaluator() },
                }
            },
            { MathOperatorType.BitwiseOr, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalBitwiseOrEvaluator() },
                    {typeof(double), new MathDoubleBitwiseOrEvaluator() },
                    {typeof(float), new MathFloatBitwiseOrEvaluator() },
                    {typeof(long), new MathInt64BitwiseOrEvaluator() },
                    {typeof(int), new MathInt32BitwiseOrEvaluator() },
                    {typeof(short), new MathInt16BitwiseOrEvaluator() },
                    {typeof(sbyte), new MathInt8BitwiseOrEvaluator() },
                    {typeof(decimal?), new MathNDecimalBitwiseOrEvaluator() },
                    {typeof(double?), new MathNDoubleBitwiseOrEvaluator() },
                    {typeof(float?), new MathNFloatBitwiseOrEvaluator() },
                    {typeof(long?), new MathNInt64BitwiseOrEvaluator() },
                    {typeof(int?), new MathNInt32BitwiseOrEvaluator() },
                    {typeof(short?), new MathNInt16BitwiseOrEvaluator() },
                    {typeof(sbyte?), new MathNInt8BitwiseOrEvaluator() },
                }
            },
            { MathOperatorType.BitwiseXor, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalBitwiseXorEvaluator() },
                    {typeof(double), new MathDoubleBitwiseXorEvaluator() },
                    {typeof(float), new MathFloatBitwiseXorEvaluator() },
                    {typeof(long), new MathInt64BitwiseXorEvaluator() },
                    {typeof(int), new MathInt32BitwiseXorEvaluator() },
                    {typeof(short), new MathInt16BitwiseXorEvaluator() },
                    {typeof(sbyte), new MathInt8BitwiseXorEvaluator() },
                    {typeof(decimal?), new MathNDecimalBitwiseXorEvaluator() },
                    {typeof(double?), new MathNDoubleBitwiseXorEvaluator() },
                    {typeof(float?), new MathNFloatBitwiseXorEvaluator() },
                    {typeof(long?), new MathNInt64BitwiseXorEvaluator() },
                    {typeof(int?), new MathNInt32BitwiseXorEvaluator() },
                    {typeof(short?), new MathNInt16BitwiseXorEvaluator() },
                    {typeof(sbyte?), new MathNInt8BitwiseXorEvaluator() },
                }
            },
            { MathOperatorType.Power, new Dictionary<Type, IEvaluator>
                {
                    {typeof(decimal), new MathDecimalPowerEvaluator() },
                    {typeof(double), new MathDoublePowerEvaluator() },
                    {typeof(float), new MathFloatPowerEvaluator() },
                    {typeof(long), new MathInt64PowerEvaluator() },
                    {typeof(int), new MathInt32PowerEvaluator() },
                    {typeof(short), new MathInt16PowerEvaluator() },
                    {typeof(sbyte), new MathInt8PowerEvaluator() },
                    {typeof(decimal?), new MathNDecimalPowerEvaluator() },
                    {typeof(double?), new MathNDoublePowerEvaluator() },
                    {typeof(float?), new MathNFloatPowerEvaluator() },
                    {typeof(long?), new MathNInt64PowerEvaluator() },
                    {typeof(int?), new MathNInt32PowerEvaluator() },
                    {typeof(short?), new MathNInt16PowerEvaluator() },
                    {typeof(sbyte?), new MathNInt8PowerEvaluator() },
                }
            },
        };

}

