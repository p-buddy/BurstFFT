namespace JamUp.NativeArrayUtility
{
    public interface INativeArrayComparisonEvent<TType, TIdentifier, TInfo>
    {
        public TType CreateComparisonEvent(TIdentifier identifierA, TIdentifier identifierB, TInfo info);
    }
}