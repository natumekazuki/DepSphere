namespace SampleFixture;

public interface IService { }

public class Base { }

public class Dependency
{
    public static void Touch() { }
}

public class Impl : Base, IService
{
    private readonly Dependency _field = new();

    public Dependency Prop { get; set; } = new();

    public Dependency Method(Dependency arg)
    {
        Dependency.Touch();
        return arg;
    }
}
