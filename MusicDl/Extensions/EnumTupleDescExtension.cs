using MusicDl.Attributes;
using System.Reflection;
using System.Windows.Markup;

namespace MusicDl.Extensions;

public class EnumTupleDescExtension : MarkupExtension
{
    private Type? _enumType;

    public EnumTupleDescExtension(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        EnumType = enumType;
    }

    public Type EnumType
    {
        get => _enumType!;
        private set
        {
            if (_enumType == value)
                return;

            var enumType = Nullable.GetUnderlyingType(value) ?? value;
            if (!enumType.IsEnum)
                throw new ArgumentException("Type must be an Enum.", nameof(value));

            _enumType = value;
        }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var enumValues = Enum.GetValues(EnumType);

        return enumValues
            .Cast<object>()
            .Select(enumValue => new EnumerationMember
            {
                Value = enumValue,
                Description = GetTupleDescription(enumValue)
            })
            .ToArray();
    }

    private string GetTupleDescription(object enumValue)
    {
        var fieldInfo = EnumType.GetField(enumValue.ToString()!);
        if (fieldInfo is null)
            return enumValue.ToString()!;

        var descriptionAttribute = fieldInfo
            .GetCustomAttribute<TupleDescAttribute>();

        return descriptionAttribute?.Key ?? enumValue.ToString()!;
    }

    public class EnumerationMember
    {
        public required string Description { get; set; }
        public required object Value { get; set; }
    }
}
