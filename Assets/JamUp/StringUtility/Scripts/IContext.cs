namespace JamUp.StringUtility
{
    public interface IContext<TObjectType>
    {
        delegate string GetContext<TObjectType>();

        string Context { get; set; }
    }
}