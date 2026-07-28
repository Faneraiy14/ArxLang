namespace ArxLang.AST;

public enum ArxType
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
    public ArxType Type { get; }
    public string? Name { get; }
    public TypeInfo? ElementType { get; }
    public List<TypeInfo>? GenericArgs { get; }
    public Dictionary<string, TypeInfo>? Fields { get; }

    public TypeInfo(ArxType type, string? name = null, TypeInfo? elementType = null, 
                    List<TypeInfo>? genericArgs = null, Dictionary<string, TypeInfo>? fields = null)
    {
        Type = type;
        Name = name;
        ElementType = elementType;
        GenericArgs = genericArgs;
        Fields = fields;
    }

    public bool IsNumeric() => Type == ArxType.Int32 || Type == ArxType.Int64 || 
                                Type == ArxType.Float32 || Type == ArxType.Float64;
    
    public bool IsInteger() => Type == ArxType.Int32 || Type == ArxType.Int64;
    public bool IsFloat() => Type == ArxType.Float32 || Type == ArxType.Float64;
    public bool IsComparable() => IsNumeric() || Type == ArxType.String || Type == ArxType.Boolean;
    public bool IsPrimitive() => IsNumeric() || Type == ArxType.String || Type == ArxType.Boolean || Type == ArxType.Char;

    public static TypeInfo Int32() => new TypeInfo(ArxType.Int32, "i32");
    public static TypeInfo Int64() => new TypeInfo(ArxType.Int64, "i64");
    public static TypeInfo Float32() => new TypeInfo(ArxType.Float32, "f32");
    public static TypeInfo Float64() => new TypeInfo(ArxType.Float64, "f64");
    public static TypeInfo Boolean() => new TypeInfo(ArxType.Boolean, "bool");
    public static TypeInfo String() => new TypeInfo(ArxType.String, "string");
    public static TypeInfo Char() => new TypeInfo(ArxType.Char, "char");
    public static TypeInfo Void() => new TypeInfo(ArxType.Void, "void");
    public static TypeInfo Any() => new TypeInfo(ArxType.Any, "any");
    public static TypeInfo Array(TypeInfo elementType) => new TypeInfo(ArxType.Array, $"[{elementType.Name}]", elementType);

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