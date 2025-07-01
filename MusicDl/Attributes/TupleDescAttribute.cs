using System.Diagnostics.CodeAnalysis;

namespace MusicDl.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class TupleDescAttribute(string key, string value) : Attribute
{
    public static readonly TupleDescAttribute Default = new();

    public virtual string Key => KeyProp;

    protected string KeyProp { get; set; } = key;

    public virtual string Value => ValueProp;

    protected string ValueProp { get; set; } = value;

    public TupleDescAttribute() : this(string.Empty, string.Empty)
    {
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is TupleDescAttribute attribute)
        {
            return attribute.Key == Key && attribute.Value == Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }

    public override bool IsDefaultAttribute()
    {
        return Equals(Default);
    }
}
