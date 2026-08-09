namespace BunnyTail.MemberAccessor;

// Getter/setter delegates take the target by reference so a single shape covers
// both reference types (no cost) and value types (no copy, in-place mutation).
public delegate TProperty Getter<T, TProperty>(ref T target);

public delegate void Setter<T, TProperty>(ref T target, TProperty value);
