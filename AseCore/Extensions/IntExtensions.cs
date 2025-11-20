namespace AseCore.Extensions
{
    public static class IntExtensions
    {
        public static int DigitCount(this int number)
        {
            var strlen = number.ToString().Length;
            return number < 0 ? strlen - 1 : strlen;
        }
    }
}