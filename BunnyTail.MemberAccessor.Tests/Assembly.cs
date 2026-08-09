using BunnyTail.MemberAccessor;

[assembly: CLSCompliant(false)]

[assembly: TypedAccessor(typeof(GenericData<string>))]
[assembly: TypedAccessor(typeof(GenericData<double>))]

[assembly: GenerateAccessorFor(typeof(Version))]
