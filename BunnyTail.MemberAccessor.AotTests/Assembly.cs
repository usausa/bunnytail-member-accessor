using BunnyTail.MemberAccessor;
using BunnyTail.MemberAccessor.AotTests;

// Pre-register closed generics types for Activator.CreateInstance/MakeGenericType
// This is the AOT-safe path for generic types
[assembly: TypedAccessor(typeof(GenericData<int>))]
[assembly: TypedAccessor(typeof(GenericData<string>))]
[assembly: TypedAccessor(typeof(GenericHolder<int>))]
