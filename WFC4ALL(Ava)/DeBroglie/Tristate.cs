namespace WFC4All.DeBroglie
{
    internal enum Tristate
    {
        NO = -1,
        MAYBE = 0,
        YES = 1,
    }

    internal static class TristateExtensions
    {
        public static bool isYes(this Tristate v) => v == Tristate.YES;
        public static bool isMaybe(this Tristate v) => v == Tristate.MAYBE;
        public static bool isNo(this Tristate v) => v == Tristate.NO;
        public static bool impossible(this Tristate v) => v == Tristate.NO;
        public static bool possible(this Tristate v) => v != Tristate.NO;
    }
}
