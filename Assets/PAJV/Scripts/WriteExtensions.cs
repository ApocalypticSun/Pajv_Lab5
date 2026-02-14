using DarkRift;

namespace PAJV.Net
{
    public static class WriteExt
    {
        public static void WriteVec3(this DarkRiftWriter w, float x, float y, float z)
        {
            w.Write(x);
            w.Write(y);
            w.Write(z);
        }
    }
}
