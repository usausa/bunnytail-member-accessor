namespace BunnyTail.MemberAccessor;

using System.Runtime.CompilerServices;

public static class AccessorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TProperty Read<T, TProperty>(this Getter<T, TProperty> getter, in T target)
        where T : class =>
        getter(ref Unsafe.AsRef(in target));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T, TProperty>(this Setter<T, TProperty> setter, in T target, TProperty value)
        where T : class =>
        setter(ref Unsafe.AsRef(in target), value);
}
