namespace BunnyTail.MemberAccessor;

using System.Runtime.CompilerServices;

public static class AccessorExtensions
{
    // Reference types can be accessed without the caller supplying a variable: the delegate
    // mutates through the reference, so the caller's object is updated without a write-back.
    // This lets callers pass expressions that ref forbids (properties, list elements, foreach
    // variables). The class constraint keeps value types, where this would silently update a
    // copy, out at compile time.
    //
    // The target is taken by 'in' so that an existing variable is passed by address instead of
    // being spilled to a temporary; generated accessors only assign to members of the target,
    // never to the target itself, so discarding readonly-ness here is safe.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TProperty Read<T, TProperty>(this Getter<T, TProperty> getter, in T target)
        where T : class =>
        getter(ref Unsafe.AsRef(in target));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T, TProperty>(this Setter<T, TProperty> setter, in T target, TProperty value)
        where T : class =>
        setter(ref Unsafe.AsRef(in target), value);
}
