namespace Engine;

[AttributeUsage(AttributeTargets.Field)]
public class HeaderAttribute : Attribute
{
    public string Label { get; set; }

    public HeaderAttribute(string label)
    {
        Label = label;
    }
}