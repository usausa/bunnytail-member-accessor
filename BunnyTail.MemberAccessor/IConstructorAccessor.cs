namespace BunnyTail.MemberAccessor;

public interface IConstructorAccessor<out T>
{
    // Creates an instance using the parameterless constructor.
    T Create();

    // Creates an instance using a 1-parameter constructor.
    T Create<TArg>(TArg arg);

    // Creates an instance using a 2-parameter constructor.
    T Create<TArg1, TArg2>(TArg1 arg1, TArg2 arg2);

    // Creates an instance using a 3-parameter constructor.
    T Create<TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3);

    // Creates an instance using a 4-parameter constructor.
    T Create<TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
}
