namespace LCY
{
    public interface ILerpable<T>
    {
        T Lerp(T a, T b, float t);
    }
}
