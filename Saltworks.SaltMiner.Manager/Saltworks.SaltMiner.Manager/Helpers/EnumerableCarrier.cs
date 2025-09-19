namespace Saltworks.SaltMiner.Manager.Helpers
{
    internal class EnumerableCarrier<T>(T value, long? totalHits) where T: class
    {
        internal long? TotalHits { get; } = totalHits;
        internal T Value { get; } = value;
    }
}
