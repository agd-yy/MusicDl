using System.ComponentModel;
using System.Reflection;
using System.Windows.Markup;

namespace MusicDl.Extensions;

public class EnumerationExtension : MarkupExtension
{
    private Type? _enumType;

    public EnumerationExtension(Type enumType)
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
                Description = GetDescription(enumValue)
            })
            .ToArray();
    }

    private string GetDescription(object enumValue)
    {
        var fieldInfo = EnumType.GetField(enumValue.ToString()!);
        if (fieldInfo is null)
            return enumValue.ToString()!;

        var descriptionAttribute = fieldInfo
            .GetCustomAttribute<DescriptionAttribute>();

        return descriptionAttribute?.Description ?? enumValue.ToString()!;
    }

    public class EnumerationMember
    {
        public required string Description { get; set; }
        public required object Value { get; set; }
    }
}
