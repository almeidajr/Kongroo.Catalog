namespace Kongroo.BuildingBlocks.Infrastructure;

public static class TypeExtensions
{
    extension(Type type)
    {
        public string ToDisplayName()
        {
            var typeName = GetTypeNameWithoutGenericArity(type);

            return type.IsGenericType ? $"{typeName}<{FormatGenericArguments(type)}>" : typeName;
        }
    }

    private static string GetTypeNameWithoutGenericArity(Type type)
    {
        var arityIndex = type.Name.IndexOf('`', StringComparison.Ordinal);

        return arityIndex >= 0 ? type.Name[..arityIndex] : type.Name;
    }

    private static string FormatGenericArguments(Type type)
    {
        var genericArguments = type.GetGenericArguments().Select(argument => argument.ToDisplayName());

        return string.Join(", ", genericArguments);
    }
}
