namespace DataWF.Invokers;

public interface IRuntimeConstructor
{
    string Types { get; }
    object? Create();
    object? Create(params object[]? args);
}

public interface IRuntimeConstructor<out T> : IRuntimeConstructor
    where T: allows ref struct
{
    new T Create();
    new T Create(params object[]? args);
}

public interface IRuntimeConstructor<out T, P1> : IRuntimeConstructor<T>
    where P1: allows ref struct
{
    T Create(P1 p1);
}

public interface IRuntimeConstructor<out T, P1, P2> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
{
    T Create(P1 p1, P2 p2);
}

public interface IRuntimeConstructor<out T, P1, P2, P3> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3);
}

public interface IRuntimeConstructor<out T, P1, P2, P3, P4> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
    where P4: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3, P4 p4);
}

public interface IRuntimeConstructor<out T, P1, P2, P3, P4, P5> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
    where P4: allows ref struct
    where P5: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5);
}

public interface IRuntimeConstructor<out T, P1, P2, P3, P4, P5, P6> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
    where P4: allows ref struct
    where P5: allows ref struct
    where P6: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6);
}

public interface IRuntimeConstructor<out T, P1, P2, P3, P4, P5, P6, P7> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
    where P4: allows ref struct
    where P5: allows ref struct
    where P7: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7);
}

public interface IRuntimeConstructor<out T, P1, P2, P3, P4, P5, P6, P7, P8> : IRuntimeConstructor<T>
    where P1: allows ref struct
    where P2: allows ref struct
    where P3: allows ref struct
    where P4: allows ref struct
    where P5: allows ref struct
    where P7: allows ref struct
    where P8: allows ref struct
{
    T Create(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8);
}