namespace BunnyTail.MemberAccessor;

public delegate TProperty Getter<T, out TProperty>(ref T target);

public delegate void Setter<T, in TProperty>(ref T target, TProperty value);
