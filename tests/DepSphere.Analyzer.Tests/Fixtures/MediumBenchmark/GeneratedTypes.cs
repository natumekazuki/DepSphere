namespace MediumBenchmark;

public interface INode
{
}

public abstract class NodeBase
{
    public virtual int Id => 0;
}

public class C000 : NodeBase, INode
{
    private readonly C001 _next = new();

    public C002? Link { get; set; }

    public C002 Run(C002 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C001();
        return input;
    }
}

public class C001 : NodeBase, INode
{
    private readonly C002 _next = new();

    public C003? Link { get; set; }

    public C003 Run(C003 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C002();
        return input;
    }
}

public class C002 : NodeBase, INode
{
    private readonly C003 _next = new();

    public C004? Link { get; set; }

    public C004 Run(C004 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C003();
        return input;
    }
}

public class C003 : NodeBase, INode
{
    private readonly C004 _next = new();

    public C005? Link { get; set; }

    public C005 Run(C005 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C004();
        return input;
    }
}

public class C004 : NodeBase, INode
{
    private readonly C005 _next = new();

    public C006? Link { get; set; }

    public C006 Run(C006 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C005();
        return input;
    }
}

public class C005 : NodeBase, INode
{
    private readonly C006 _next = new();

    public C007? Link { get; set; }

    public C007 Run(C007 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C006();
        return input;
    }
}

public class C006 : NodeBase, INode
{
    private readonly C007 _next = new();

    public C008? Link { get; set; }

    public C008 Run(C008 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C007();
        return input;
    }
}

public class C007 : NodeBase, INode
{
    private readonly C008 _next = new();

    public C009? Link { get; set; }

    public C009 Run(C009 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C008();
        return input;
    }
}

public class C008 : NodeBase, INode
{
    private readonly C009 _next = new();

    public C010? Link { get; set; }

    public C010 Run(C010 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C009();
        return input;
    }
}

public class C009 : NodeBase, INode
{
    private readonly C010 _next = new();

    public C011? Link { get; set; }

    public C011 Run(C011 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C010();
        return input;
    }
}

public class C010 : NodeBase, INode
{
    private readonly C011 _next = new();

    public C012? Link { get; set; }

    public C012 Run(C012 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C011();
        return input;
    }
}

public class C011 : NodeBase, INode
{
    private readonly C012 _next = new();

    public C013? Link { get; set; }

    public C013 Run(C013 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C012();
        return input;
    }
}

public class C012 : NodeBase, INode
{
    private readonly C013 _next = new();

    public C014? Link { get; set; }

    public C014 Run(C014 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C013();
        return input;
    }
}

public class C013 : NodeBase, INode
{
    private readonly C014 _next = new();

    public C015? Link { get; set; }

    public C015 Run(C015 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C014();
        return input;
    }
}

public class C014 : NodeBase, INode
{
    private readonly C015 _next = new();

    public C016? Link { get; set; }

    public C016 Run(C016 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C015();
        return input;
    }
}

public class C015 : NodeBase, INode
{
    private readonly C016 _next = new();

    public C017? Link { get; set; }

    public C017 Run(C017 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C016();
        return input;
    }
}

public class C016 : NodeBase, INode
{
    private readonly C017 _next = new();

    public C018? Link { get; set; }

    public C018 Run(C018 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C017();
        return input;
    }
}

public class C017 : NodeBase, INode
{
    private readonly C018 _next = new();

    public C019? Link { get; set; }

    public C019 Run(C019 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C018();
        return input;
    }
}

public class C018 : NodeBase, INode
{
    private readonly C019 _next = new();

    public C020? Link { get; set; }

    public C020 Run(C020 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C019();
        return input;
    }
}

public class C019 : NodeBase, INode
{
    private readonly C020 _next = new();

    public C021? Link { get; set; }

    public C021 Run(C021 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C020();
        return input;
    }
}

public class C020 : NodeBase, INode
{
    private readonly C021 _next = new();

    public C022? Link { get; set; }

    public C022 Run(C022 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C021();
        return input;
    }
}

public class C021 : NodeBase, INode
{
    private readonly C022 _next = new();

    public C023? Link { get; set; }

    public C023 Run(C023 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C022();
        return input;
    }
}

public class C022 : NodeBase, INode
{
    private readonly C023 _next = new();

    public C024? Link { get; set; }

    public C024 Run(C024 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C023();
        return input;
    }
}

public class C023 : NodeBase, INode
{
    private readonly C024 _next = new();

    public C025? Link { get; set; }

    public C025 Run(C025 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C024();
        return input;
    }
}

public class C024 : NodeBase, INode
{
    private readonly C025 _next = new();

    public C026? Link { get; set; }

    public C026 Run(C026 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C025();
        return input;
    }
}

public class C025 : NodeBase, INode
{
    private readonly C026 _next = new();

    public C027? Link { get; set; }

    public C027 Run(C027 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C026();
        return input;
    }
}

public class C026 : NodeBase, INode
{
    private readonly C027 _next = new();

    public C028? Link { get; set; }

    public C028 Run(C028 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C027();
        return input;
    }
}

public class C027 : NodeBase, INode
{
    private readonly C028 _next = new();

    public C029? Link { get; set; }

    public C029 Run(C029 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C028();
        return input;
    }
}

public class C028 : NodeBase, INode
{
    private readonly C029 _next = new();

    public C030? Link { get; set; }

    public C030 Run(C030 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C029();
        return input;
    }
}

public class C029 : NodeBase, INode
{
    private readonly C030 _next = new();

    public C031? Link { get; set; }

    public C031 Run(C031 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C030();
        return input;
    }
}

public class C030 : NodeBase, INode
{
    private readonly C031 _next = new();

    public C032? Link { get; set; }

    public C032 Run(C032 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C031();
        return input;
    }
}

public class C031 : NodeBase, INode
{
    private readonly C032 _next = new();

    public C033? Link { get; set; }

    public C033 Run(C033 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C032();
        return input;
    }
}

public class C032 : NodeBase, INode
{
    private readonly C033 _next = new();

    public C034? Link { get; set; }

    public C034 Run(C034 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C033();
        return input;
    }
}

public class C033 : NodeBase, INode
{
    private readonly C034 _next = new();

    public C035? Link { get; set; }

    public C035 Run(C035 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C034();
        return input;
    }
}

public class C034 : NodeBase, INode
{
    private readonly C035 _next = new();

    public C036? Link { get; set; }

    public C036 Run(C036 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C035();
        return input;
    }
}

public class C035 : NodeBase, INode
{
    private readonly C036 _next = new();

    public C037? Link { get; set; }

    public C037 Run(C037 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C036();
        return input;
    }
}

public class C036 : NodeBase, INode
{
    private readonly C037 _next = new();

    public C038? Link { get; set; }

    public C038 Run(C038 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C037();
        return input;
    }
}

public class C037 : NodeBase, INode
{
    private readonly C038 _next = new();

    public C039? Link { get; set; }

    public C039 Run(C039 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C038();
        return input;
    }
}

public class C038 : NodeBase, INode
{
    private readonly C039 _next = new();

    public C040? Link { get; set; }

    public C040 Run(C040 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C039();
        return input;
    }
}

public class C039 : NodeBase, INode
{
    private readonly C040 _next = new();

    public C041? Link { get; set; }

    public C041 Run(C041 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C040();
        return input;
    }
}

public class C040 : NodeBase, INode
{
    private readonly C041 _next = new();

    public C042? Link { get; set; }

    public C042 Run(C042 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C041();
        return input;
    }
}

public class C041 : NodeBase, INode
{
    private readonly C042 _next = new();

    public C043? Link { get; set; }

    public C043 Run(C043 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C042();
        return input;
    }
}

public class C042 : NodeBase, INode
{
    private readonly C043 _next = new();

    public C044? Link { get; set; }

    public C044 Run(C044 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C043();
        return input;
    }
}

public class C043 : NodeBase, INode
{
    private readonly C044 _next = new();

    public C045? Link { get; set; }

    public C045 Run(C045 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C044();
        return input;
    }
}

public class C044 : NodeBase, INode
{
    private readonly C045 _next = new();

    public C046? Link { get; set; }

    public C046 Run(C046 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C045();
        return input;
    }
}

public class C045 : NodeBase, INode
{
    private readonly C046 _next = new();

    public C047? Link { get; set; }

    public C047 Run(C047 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C046();
        return input;
    }
}

public class C046 : NodeBase, INode
{
    private readonly C047 _next = new();

    public C048? Link { get; set; }

    public C048 Run(C048 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C047();
        return input;
    }
}

public class C047 : NodeBase, INode
{
    private readonly C048 _next = new();

    public C049? Link { get; set; }

    public C049 Run(C049 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C048();
        return input;
    }
}

public class C048 : NodeBase, INode
{
    private readonly C049 _next = new();

    public C050? Link { get; set; }

    public C050 Run(C050 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C049();
        return input;
    }
}

public class C049 : NodeBase, INode
{
    private readonly C050 _next = new();

    public C051? Link { get; set; }

    public C051 Run(C051 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C050();
        return input;
    }
}

public class C050 : NodeBase, INode
{
    private readonly C051 _next = new();

    public C052? Link { get; set; }

    public C052 Run(C052 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C051();
        return input;
    }
}

public class C051 : NodeBase, INode
{
    private readonly C052 _next = new();

    public C053? Link { get; set; }

    public C053 Run(C053 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C052();
        return input;
    }
}

public class C052 : NodeBase, INode
{
    private readonly C053 _next = new();

    public C054? Link { get; set; }

    public C054 Run(C054 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C053();
        return input;
    }
}

public class C053 : NodeBase, INode
{
    private readonly C054 _next = new();

    public C055? Link { get; set; }

    public C055 Run(C055 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C054();
        return input;
    }
}

public class C054 : NodeBase, INode
{
    private readonly C055 _next = new();

    public C056? Link { get; set; }

    public C056 Run(C056 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C055();
        return input;
    }
}

public class C055 : NodeBase, INode
{
    private readonly C056 _next = new();

    public C057? Link { get; set; }

    public C057 Run(C057 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C056();
        return input;
    }
}

public class C056 : NodeBase, INode
{
    private readonly C057 _next = new();

    public C058? Link { get; set; }

    public C058 Run(C058 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C057();
        return input;
    }
}

public class C057 : NodeBase, INode
{
    private readonly C058 _next = new();

    public C059? Link { get; set; }

    public C059 Run(C059 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C058();
        return input;
    }
}

public class C058 : NodeBase, INode
{
    private readonly C059 _next = new();

    public C060? Link { get; set; }

    public C060 Run(C060 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C059();
        return input;
    }
}

public class C059 : NodeBase, INode
{
    private readonly C060 _next = new();

    public C061? Link { get; set; }

    public C061 Run(C061 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C060();
        return input;
    }
}

public class C060 : NodeBase, INode
{
    private readonly C061 _next = new();

    public C062? Link { get; set; }

    public C062 Run(C062 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C061();
        return input;
    }
}

public class C061 : NodeBase, INode
{
    private readonly C062 _next = new();

    public C063? Link { get; set; }

    public C063 Run(C063 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C062();
        return input;
    }
}

public class C062 : NodeBase, INode
{
    private readonly C063 _next = new();

    public C064? Link { get; set; }

    public C064 Run(C064 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C063();
        return input;
    }
}

public class C063 : NodeBase, INode
{
    private readonly C064 _next = new();

    public C065? Link { get; set; }

    public C065 Run(C065 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C064();
        return input;
    }
}

public class C064 : NodeBase, INode
{
    private readonly C065 _next = new();

    public C066? Link { get; set; }

    public C066 Run(C066 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C065();
        return input;
    }
}

public class C065 : NodeBase, INode
{
    private readonly C066 _next = new();

    public C067? Link { get; set; }

    public C067 Run(C067 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C066();
        return input;
    }
}

public class C066 : NodeBase, INode
{
    private readonly C067 _next = new();

    public C068? Link { get; set; }

    public C068 Run(C068 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C067();
        return input;
    }
}

public class C067 : NodeBase, INode
{
    private readonly C068 _next = new();

    public C069? Link { get; set; }

    public C069 Run(C069 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C068();
        return input;
    }
}

public class C068 : NodeBase, INode
{
    private readonly C069 _next = new();

    public C070? Link { get; set; }

    public C070 Run(C070 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C069();
        return input;
    }
}

public class C069 : NodeBase, INode
{
    private readonly C070 _next = new();

    public C071? Link { get; set; }

    public C071 Run(C071 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C070();
        return input;
    }
}

public class C070 : NodeBase, INode
{
    private readonly C071 _next = new();

    public C072? Link { get; set; }

    public C072 Run(C072 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C071();
        return input;
    }
}

public class C071 : NodeBase, INode
{
    private readonly C072 _next = new();

    public C073? Link { get; set; }

    public C073 Run(C073 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C072();
        return input;
    }
}

public class C072 : NodeBase, INode
{
    private readonly C073 _next = new();

    public C074? Link { get; set; }

    public C074 Run(C074 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C073();
        return input;
    }
}

public class C073 : NodeBase, INode
{
    private readonly C074 _next = new();

    public C075? Link { get; set; }

    public C075 Run(C075 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C074();
        return input;
    }
}

public class C074 : NodeBase, INode
{
    private readonly C075 _next = new();

    public C076? Link { get; set; }

    public C076 Run(C076 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C075();
        return input;
    }
}

public class C075 : NodeBase, INode
{
    private readonly C076 _next = new();

    public C077? Link { get; set; }

    public C077 Run(C077 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C076();
        return input;
    }
}

public class C076 : NodeBase, INode
{
    private readonly C077 _next = new();

    public C078? Link { get; set; }

    public C078 Run(C078 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C077();
        return input;
    }
}

public class C077 : NodeBase, INode
{
    private readonly C078 _next = new();

    public C079? Link { get; set; }

    public C079 Run(C079 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C078();
        return input;
    }
}

public class C078 : NodeBase, INode
{
    private readonly C079 _next = new();

    public C000? Link { get; set; }

    public C000 Run(C000 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C079();
        return input;
    }
}

public class C079 : NodeBase, INode
{
    private readonly C000 _next = new();

    public C001? Link { get; set; }

    public C001 Run(C001 input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        _ = new C000();
        return input;
    }
}

