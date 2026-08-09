using BunnyTail.MemberAccessor;
using BunnyTail.MemberAccessor.AotTests;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine($"FAIL: {message}");
        Environment.Exit(1);
    }
}

Console.WriteLine("BunnyTail.MemberAccessor AOT smoke tests starting...");

//--------------------------------------------------------------------------------
// 1. IAccessor get/set on a reference type
//--------------------------------------------------------------------------------
{
    var accessor = AccessorRegistry.FindAccessor<Data>();
    Assert(accessor is not null, "FindAccessor<Data> returned null");

    var data = new Data { Id = 1, Name = "Alice" };
    Assert(Equals(accessor!.GetValue(data, nameof(Data.Id)), 1), "Accessor GetValue Id");
    Assert(Equals(accessor.GetValue(data, nameof(Data.Name)), "Alice"), "Accessor GetValue Name");

    accessor.SetValue(data, nameof(Data.Id), 2);
    accessor.SetValue(data, nameof(Data.Name), "Bob");
    Assert(data.Id == 2, "Accessor SetValue Id");
    Assert(data.Name == "Bob", "Accessor SetValue Name");
    Console.WriteLine("  [OK] IAccessor get/set (reference type)");
}

//--------------------------------------------------------------------------------
// 2. IAccessorFactory typed getter/setter delegates
//--------------------------------------------------------------------------------
{
    var factory = AccessorRegistry.FindFactory<Data>();
    Assert(factory is not null, "FindFactory<Data> returned null");

    var getId = factory!.CreateGetter<int>(nameof(Data.Id));
    var setName = factory.CreateSetter<string>(nameof(Data.Name));
    Assert(getId is not null, "CreateGetter<int>(Id) returned null");
    Assert(setName is not null, "CreateSetter<string>(Name) returned null");

    var data = new Data { Id = 42, Name = "x" };
    Assert(getId!(ref data) == 42, "Typed getter Id");
    setName!(ref data, "y");
    Assert(data.Name == "y", "Typed setter Name");
    Console.WriteLine("  [OK] IAccessorFactory typed delegates");
}

//--------------------------------------------------------------------------------
// 3. MemberDescriptor metadata
//--------------------------------------------------------------------------------
{
    var factory = AccessorRegistry.FindFactory<Data>();
    Assert(factory is not null, "FindFactory<Data> returned null");

    var members = factory!.Members;
    Assert(members.Count == 2, "Members count");
    var id = members.FirstOrDefault(m => m.Name == nameof(Data.Id));
    Assert(id is not null, "Member Id present");
    Assert(id!.Type == typeof(int), "Member Id type");
    Assert(id.CanRead && id.CanWrite, "Member Id read/write flags");
    Console.WriteLine("  [OK] MemberDescriptor metadata");
}

//--------------------------------------------------------------------------------
// 4. Value type (struct) accessor via boxed instance
//--------------------------------------------------------------------------------
{
    var accessor = AccessorRegistry.FindAccessor<StructData>();
    Assert(accessor is not null, "FindAccessor<StructData> returned null");

    object boxed = new StructData { Id = 7, Name = "s" };
    Assert(Equals(accessor!.GetValue(boxed, nameof(StructData.Id)), 7), "Struct GetValue Id");

    // SetValue mutates the boxed instance in place (Unsafe.Unbox).
    accessor.SetValue(boxed, nameof(StructData.Id), 8);
    Assert(((StructData)boxed).Id == 8, "Struct SetValue Id (boxed)");
    Console.WriteLine("  [OK] Value type accessor (boxed)");
}

//--------------------------------------------------------------------------------
// 4b. Value type (struct) ref-based typed setter (no boxing)
//--------------------------------------------------------------------------------
{
    var factory = AccessorRegistry.FindFactory<StructData>();
    Assert(factory is not null, "FindFactory<StructData> returned null");

    var setId = factory!.CreateSetter<int>(nameof(StructData.Id));
    Assert(setId is not null, "CreateSetter<int>(Id) returned null for struct");

    var data = new StructData { Id = 1, Name = "s" };
    setId!(ref data, 2);
    Assert(data.Id == 2, "Struct typed setter (ref, in place)");
    Console.WriteLine("  [OK] Value type ref-based typed setter");
}

//--------------------------------------------------------------------------------
// 5. init-only property is read-only
//--------------------------------------------------------------------------------
{
    var factory = AccessorRegistry.FindFactory<InitOnlyData>();
    Assert(factory is not null, "FindFactory<InitOnlyData> returned null");

    var nameMember = factory!.Members.First(m => m.Name == nameof(InitOnlyData.Name));
    Assert(nameMember.CanRead, "InitOnly Name CanRead");
    Assert(!nameMember.CanWrite, "InitOnly Name should be read-only");
    Console.WriteLine("  [OK] init-only property treated as read-only");
}

//--------------------------------------------------------------------------------
// 6. Inherited properties are collected
//--------------------------------------------------------------------------------
{
    var accessor = AccessorRegistry.FindAccessor<DerivedData>();
    Assert(accessor is not null, "FindAccessor<DerivedData> returned null");

    var data = new DerivedData { Id = 5, Name = "derived" };
    Assert(Equals(accessor!.GetValue(data, nameof(BaseData.Id)), 5), "Inherited GetValue Id");
    Assert(Equals(accessor.GetValue(data, nameof(DerivedData.Name)), "derived"), "Inherited GetValue Name");
    Console.WriteLine("  [OK] Inherited property access");
}

//--------------------------------------------------------------------------------
// 7. Constructor accessor (parameterless and 2-arg)
//--------------------------------------------------------------------------------
{
    var ctor = AccessorRegistry.FindConstructor<CtorData>();
    Assert(ctor is not null, "FindConstructor<CtorData> returned null");

    var d0 = ctor!.Create();
    Assert(d0.Id == 0 && d0.Name == "default", "Ctor Create()");

    var d2 = ctor.Create(99, "hello");
    Assert(d2.Id == 99 && d2.Name == "hello", "Ctor Create(int, string)");
    Console.WriteLine("  [OK] Constructor accessor");
}

//--------------------------------------------------------------------------------
// 8. Same-arity overloaded constructor resolved by argument type
//--------------------------------------------------------------------------------
{
    var ctor = AccessorRegistry.FindConstructor<OverloadCtorData>();
    Assert(ctor is not null, "FindConstructor<OverloadCtorData> returned null");

    var fromInt = ctor!.Create(123);
    Assert(fromInt.IntValue == 123 && fromInt.StringValue is null, "Overload ctor int");

    var fromString = ctor.Create("abc");
    Assert(fromString.StringValue == "abc" && fromString.IntValue == 0, "Overload ctor string");
    Console.WriteLine("  [OK] Same-arity overloaded constructor");
}

//--------------------------------------------------------------------------------
// 9. Pre-registered closed generic accessor (AOT-safe path via [TypedAccessor])
//--------------------------------------------------------------------------------
{
    var accInt = AccessorRegistry.FindAccessor<GenericData<int>>();
    var accStr = AccessorRegistry.FindAccessor<GenericData<string>>();
    Assert(accInt is not null, "FindAccessor<GenericData<int>> returned null");
    Assert(accStr is not null, "FindAccessor<GenericData<string>> returned null");

    var di = new GenericData<int> { Value = 10 };
    accInt!.SetValue(di, nameof(GenericData<>.Value), 20);
    Assert(di.Value == 20, "Generic<int> SetValue");

    var ds = new GenericData<string> { Value = "a" };
    Assert(Equals(accStr!.GetValue(ds, nameof(GenericData<>.Value)), "a"), "Generic<string> GetValue");
    Console.WriteLine("  [OK] Pre-registered closed generic accessor");
}

//--------------------------------------------------------------------------------
// 10. Pre-registered closed generic constructor accessor
//--------------------------------------------------------------------------------
{
    var ctor = AccessorRegistry.FindConstructor<GenericHolder<int>>();
    Assert(ctor is not null, "FindConstructor<GenericHolder<int>> returned null");

    var instance = ctor!.Create(777);
    Assert(instance.Value == 777, "GenericHolder<int> Create(value)");
    Console.WriteLine("  [OK] Pre-registered closed generic constructor");
}

//--------------------------------------------------------------------------------
// 11. IAccessorProvider / IConstructorProvider static abstract path (registry-free)
//--------------------------------------------------------------------------------
{
    static IAccessorFactory<T> GetFactory<T>()
        where T : IAccessorProvider<T>
        => T.AccessorFactory;
    static IConstructor<T> GetConstructor<T>()
        where T : IConstructorProvider<T>
        => T.Constructor;

    var factory = GetFactory<Data>();
    var data = new Data { Id = 1, Name = "a" };
    var getId = factory.CreateGetter<int>(nameof(Data.Id));
    Assert(getId is not null, "IAccessorProvider factory CreateGetter returned null");
    Assert(getId!(ref data) == 1, "IAccessorProvider typed getter");

    var ctor = GetConstructor<CtorData>();
    var created = ctor.Create(5, "c");
    Assert(created.Id == 5 && created.Name == "c", "IConstructorProvider Create");

    // Closed generic flows through the type system: works without pre-registration
    var genericFactory = GetFactory<GenericData<long>>();
    var genericData = new GenericData<long> { Value = 9 };
    var getValue = genericFactory.CreateGetter<long>(nameof(GenericData<>.Value));
    Assert(getValue is not null, "IAccessorProvider generic CreateGetter returned null");
    Assert(getValue!(ref genericData) == 9, "IAccessorProvider closed generic getter");
    Console.WriteLine("  [OK] IAccessorProvider / IConstructorProvider static abstract path");
}

//--------------------------------------------------------------------------------
// 12. Unregistered type lookup (module-initializer fallback must not throw on AOT)
//--------------------------------------------------------------------------------
{
    // On a lookup miss the registry forces the target module initializer via
    // RuntimeHelpers.RunModuleConstructor; on Native AOT this may be unsupported
    // and must be swallowed, resolving to null instead of crashing.
    static Type GetRuntimeType<T>() => typeof(T);

    Assert(AccessorRegistry.FindAccessor(GetRuntimeType<Uri>()) is null, "Unregistered FindAccessor(Type) should be null");
    Assert(AccessorRegistry.FindFactory<Uri>() is null, "Unregistered FindFactory<T> should be null");
    Console.WriteLine("  [OK] Unregistered type lookup (no crash, returns null)");
}

Console.WriteLine("All AOT smoke tests passed.");
