using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using TACBuilder.ILReflection;
using TACBuilder.Utils;

namespace TACBuilder.Exprs;

public interface IlConstant : IlSimpleValue
{
    public static IlConstant From(object? obj)
    {
        if (obj is null or DBNull) return new IlNullConst(IlInstanceBuilder.GetType(typeof(object)));
        if (obj.GetType().IsEnum)
        {
            return new IlEnumConst((IlEnumType)IlInstanceBuilder.GetType(obj.GetType()),
                From(Convert.ChangeType(obj, obj.GetType().GetEnumUnderlyingType())));
        }

        var resultConst = obj switch
        {
            string strValue => new IlStringConst(strValue),
            bool boolValue => new IlBoolConst(boolValue),
            sbyte sbyteValue => new IlInt8Const(sbyteValue),
            byte byteValue => new IlUint8Const(byteValue),
            char charValue => new IlCharConst(charValue),
            short shortValue => new IlInt16Const(shortValue),
            ushort ushortValue => new IlUint16Const(ushortValue),
            int intValue => new IlInt32Const(intValue),
            uint uintValue => new IlUint32Const(uintValue),
            long longValue => new IlInt64Const(longValue),
            ulong ulongValue => new IlUint64Const(ulongValue),
            float floatValue => new IlFloatConst(floatValue),
            double doubleValue => new IlDoubleConst(doubleValue),
            nint nintValue => new IlInt64Const(nintValue),
            Type type => new IlTypeRef(IlInstanceBuilder.GetType(type)),
            MethodBase method => new IlMethodRef(IlInstanceBuilder.GetMethod(method)),
            CustomAttributeTypedArgument attr => From(attr.Value),
            ReadOnlyCollection<CustomAttributeTypedArgument> coll => ResolveArrayConst(coll),
            _ => null
        };
        if (resultConst == null && obj is ICollection collection) return ResolveArrayConst(collection);
        return resultConst ?? throw new Exception("unexpected const of type " + obj.GetType().Name);
    }

    public static IlConstant BrFalseWith(IlExpr expr)
    {
        var t = expr.Type;
        if (t is IlEnumType enumType) t = enumType.UnderlyingType;
        if (t is IlPrimitiveType) t = expr.Coerced().Type;
        if (t is IlReferenceType rt) return new IlNullConst(rt);
        if (t is IlPointerType pt) return new IlNullConst(pt.TargetType);
        if (t.Equals(IlInstanceBuilder.GetType(typeof(bool)))) return new IlBoolConst(false);
        return new IlInt32Const(0);
    }

    private static IlArrayConst ResolveArrayConst(ICollection collection)
    {
        var values = (from object? v in collection select From(v)).ToList();
        if (collection == null) throw new Exception("unexpected array constant " + collection);
        return new IlArrayConst(
            (IlInstanceBuilder.GetType(collection.GetType().GetGenericArguments()[0].MakeArrayType()) as IlArrayType)!,
            values);
    }
}

public class IlNullConst(IlType type) : IlConstant
{
    public IlType Type => type;

    public override string ToString() => "null";
}

public class IlBoolConst(bool value) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(bool));
    public bool Value => value;
    public override string ToString() => value.ToString();
}

public class IlStringConst(string value) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(string));
    public string Value => value;
    public override string ToString() => value;
}

public class IlEnumConst(IlEnumType enumType, IlConstant underlyingValue) : IlConstant
{
    public IlType Type => enumType;
    public IlConstant Value => underlyingValue;

    public override string ToString()
    {
        return underlyingValue.ToString();
    }
}

public class IlArrayConst(IlArrayType arrayType, IEnumerable<IlConstant> values) : IlConstant
{
    public IlType Type => arrayType;
    public List<IlConstant> Values => values.ToList();

    public override string ToString()
    {
        return $"[{string.Join(", ", Values.Select(v => v.ToString()))}]";
    }
}

public class IlTypeRef(IlType type) : IlConstant
{
    public IlType ReferencedType => type;
    public IlType Type => IlInstanceBuilder.GetType(typeof(Type));
}

public class IlFieldRef(IlField field) : IlConstant
{
    public IlField Field { get; } = field;
    public IlType Type => IlInstanceBuilder.GetType(typeof(FieldInfo));
}

public class IlMethodRef(IlMethod method, IlExpr? receiver = null) : IlConstant
{
    public IlType Type => IlInstanceBuilder.GetType(typeof(MethodBase));
    public IlMethod Method => method;
}