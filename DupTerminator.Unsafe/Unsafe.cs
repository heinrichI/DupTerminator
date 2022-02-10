namespace DupTerminator.Unsafe
{
    public class Unsafe
    {
        public static unsafe bool BlockCompare(byte[] buffer1, byte[] buffer2, int offset, uint length)
        {
            if (buffer1 == null || buffer2 == null || buffer1.Length < offset + length || buffer2.Length < offset + length) return false;
            if (buffer1 == buffer2) return true;

            uint blockCount = length / sizeof(uint);
            fixed (byte* pBase1 = buffer1, pBase2 = buffer2)
            {
                int* ptr1 = (int*)(pBase1 + offset);
                int* ptr2 = (int*)(pBase2 + offset);
                for (int i = 0; i < blockCount; i++)
                    if (*ptr1++ != *ptr2++) return false;
            }
            for (int i = 0; i < length % sizeof(int); i++)
                if (buffer1[length - i - 1] != buffer2[length - i - 1]) return false;
            return true;
        }
    }
}