namespace System
{
    public static class IComparableExtensions
    {
        public static Boolean InRange<T>(this T val, T from, T to) where T : IComparable<T>
        { return val.CompareTo(from) >= 1 && val.CompareTo(to) <= -1; }
    }
}
