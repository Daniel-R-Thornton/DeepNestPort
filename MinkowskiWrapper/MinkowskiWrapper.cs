using System.Runtime.InteropServices;

namespace Minkowski
{
    public class MinkowskiWrapper
    {
#if _WINDOWS
        private const string MINKOWSKI_LIB = "minkowski.dll";
#else
        private const string MINKOWSKI_LIB = "minkowski.so";
#endif
        [DllImport(MINKOWSKI_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void setData(int cntA, double[] pntsA, int holesCnt, int[] holesSizes, double[] holesPoints, int cntB, double[] pntsB);

        [DllImport(MINKOWSKI_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void calculateNFP();

        [DllImport(MINKOWSKI_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void getSizes1(int[] sizes);

        [DllImport(MINKOWSKI_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void getSizes2(int[] sizes1, int[] sizes2);

        [DllImport(MINKOWSKI_LIB, CallingConvention = CallingConvention.Cdecl)]
        public static extern void getResults(double[] data, double[] holesData);
    }
}
