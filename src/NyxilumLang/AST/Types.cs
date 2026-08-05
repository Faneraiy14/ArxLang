namespace NyxilumLang.AST;

public enum NxType
{
    Any,
    Void,
    Int32,
    Int64,
    Float32,
    Float64,
    Boolean,
    String,
    Char,
    Array,
    Struct,
    Function,
    Nullable,
    Generic,
    Unknown
}

public class TypeInfo
{
    public NxType Type { get; }
    public string? Name { get; }
    public TypeInfo? ElementType { get; }
    public List<TypeInfo>? GenericArgs { get; }
    public Dictionary<string, TypeInfo>? Fields { get; }

    public TypeInfo(NxType type, string? name = null, TypeInfo? elementType = null, 
                    List<TypeInfo>? genericArgs = null, Dictionary<string, TypeInfo>? fields = null)
    {
        Type = type;
        Name = name;
        ElementType = elementType;
        GenericArgs = genericArgs;
        Fields = fields;
    }

    public bool IsNumeric() => Type == NxType.Int32 || Type == NxType.Int64 || 
                                Type == NxType.Float32 || Type == NxType.Float64;
    
    public bool IsInteger() => Type == NxType.Int32 || Type == NxType.Int64;
    public bool IsFloat() => Type == NxType.Float32 || Type == NxType.Float64;
    public bool IsComparable() => IsNumeric() || Type == NxType.String || Type == NxType.Boolean;
    public bool IsPrimitive() => IsNumeric() || Type == NxType.String || Type == NxType.Boolean || Type == NxType.Char;

    public static TypeInfo Int32() => new TypeInfo(NxType.Int32, "i32");
    public static TypeInfo Int64() => new TypeInfo(NxType.Int64, "i64");
    public static TypeInfo Float32() => new TypeInfo(NxType.Float32, "f32");
    public static TypeInfo Float64() => new TypeInfo(NxType.Float64, "f64");
    public static TypeInfo Boolean() => new TypeInfo(NxType.Boolean, "bool");
    public static TypeInfo String() => new TypeInfo(NxType.String, "string");
    public static TypeInfo Char() => new TypeInfo(NxType.Char, "char");
    public static TypeInfo Void() => new TypeInfo(NxType.Void, "void");
    public static TypeInfo Any() => new TypeInfo(NxType.Any, "any");
    public static TypeInfo Array(TypeInfo elementType) => new TypeInfo(NxType.Array, $"[{elementType.Name}]", elementType);

    public override string ToString() => Name ?? Type.ToString();
}

public class TypeParameter
{
    public string Name { get; }
    public TypeInfo? Constraint { get; }

    public TypeParameter(string name, TypeInfo? constraint = null)
    {
        Name = name;
        Constraint = constraint;
    }
}

public class GenericTypeDefinition
{
    public string Name { get; }
    public List<TypeParameter> Parameters { get; }
    public TypeInfo? Body { get; set; }

    public GenericTypeDefinition(string name, List<TypeParameter> parameters)
    {
        Name = name;
        Parameters = parameters;
    }
}