namespace DomainSharp;

public static class Extensions
{
    public static string Implode(this IEnumerable<string> list, string glue)
    {
        return string.Join(glue, list);
    }
}