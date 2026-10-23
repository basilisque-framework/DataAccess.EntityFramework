namespace Basilisque.Demo.Data;

public static class DemoUsers
{
    public static Guid Alice { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static Guid Bob { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static string Display(Guid? id) => id switch
    {
        null => "(no user)",
        var value when value == Guid.Empty => "(no user)",
        var value when value == Alice => "Alice",
        var value when value == Bob => "Bob",
        _ => id.ToString()!
    };
}
