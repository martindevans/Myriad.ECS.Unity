using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> Aliases = new()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        /// <summary>
        /// Returns the type name, in a form similar to how it would be written in C#.
        /// Correctly handles generic and nested types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public static string GetFormattedName(this Type type)
        {
            var tArgs = type.IsGenericType ? type.GetGenericArguments() : Array.Empty<Type>();
            return GetFormattedName(type, tArgs);
        }

        private static string GetFormattedName([CanBeNull] this Type type, Type[] generics)
        {
            if (type == null)
                return "";

            // Arrays
            if (type.IsArray)
                return $"{type.GetElementType().GetFormattedName()}[]";

            // Simple types
            if (!type.IsGenericType || generics.Length == 0)
            {
                if (Aliases.TryGetValue(type, out var alias))
                    return alias;

                var prefix = GetFormattedName(type.DeclaringType, generics);
                if (!string.IsNullOrWhiteSpace(prefix))
                    return $"{prefix}.{type.Name}";

                return type.Name;
            }

            // Non-nested generics
            if (!type.IsNested || type.DeclaringType == null)
            {
                var genericArguments = string.Join(", ", generics.Select(GetFormattedName));
                return $"{type.Name[..type.Name.IndexOf("`", StringComparison.Ordinal)]}<{genericArguments}>";
            }

            // Calculate how many generics are "left over" after the declaring type has taken generics
            var cOuter = type.DeclaringType.GetGenericArguments().Length;
            var cInner = type.GetGenericArguments().Length - cOuter;

            // Calculate name of inner type
            var inner = type.Name;
            if (cInner > 0)
            {
                var tInner = generics[^cInner..];
                var tInnerNames = string.Join(", ", tInner.Select(GetFormattedName));
                inner = $"{type.Name[..type.Name.IndexOf("`", StringComparison.Ordinal)]}<{tInnerNames}>";
            }

            // Calculate name of outer type
            var tOuter = generics[..cOuter];
            var outer = type.DeclaringType.GetFormattedName(tOuter);

            // Stick them together
            return $"{outer}.{inner}";
        }
    }
}