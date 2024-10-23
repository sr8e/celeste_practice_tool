namespace celeste_practice_tool
{
    public class Signature
    {
        public string pattern;
        public int offsetToPtr;
        public Signature(string pattern, int offsetToPtr)
        {
            this.pattern = pattern.ToLower();
            this.offsetToPtr = offsetToPtr;
        }

        public void ToBytes(out byte[] signArray, out bool[] wildcard, out int[] offsets)
        {
            int len = pattern.Length;
            if (len % 2 != 0)
            {
                throw new ArgumentException("invalid signature pattern; odd length");
            }
            signArray = new byte[len / 2];
            wildcard = new bool[len / 2];
            int tmp = 0;
            bool isWild = false;
            for (int i = 0; i < len; i++)
            {
                char c = pattern[i];
                int val = 0;
                if (c == '?')
                {
                    if (i % 2 == 1 && !isWild)
                    {
                        throw new ArgumentException("invalid signature pattern; partial wildcard");
                    }
                    isWild = true;
                }
                else
                {
                    isWild = false;
                    if (char.IsAsciiDigit(c))
                    {
                        val = c - '0';
                    }
                    else if (char.IsAsciiHexDigitLower(c))
                    {
                        val = c - 'a' + 10;
                    }
                    else
                    {
                        throw new ArgumentException("invalid signature pattern; invalid char");
                    }
                }
                if (i % 2 == 0)
                {
                    tmp = val;
                }
                else if (isWild)
                {
                    wildcard[i / 2] = true;
                }
                else
                {
                    signArray[i / 2] = (byte)((tmp << 4) + val);
                }
            }
            offsets = lookupOffset(signArray, wildcard);
        }

        private int[] lookupOffset(byte[] search, bool[] mask)
        {
            int[] offsets = new int[256];
            int unknown = 0;
            int end = search.Length - 1;
            for (int i = 0; i < end; i++)
            {
                if (!mask[i])
                {
                    offsets[search[i]] = end - i;
                }
                else
                {
                    unknown = end - i;
                }
            }

            if (unknown == 0)
            {
                unknown = search.Length;
            }

            for (int i = 0; i < 256; i++)
            {
                int offset = offsets[i];
                if (unknown < offset || offset == 0)
                {
                    offsets[i] = unknown;
                }
            }
            return offsets;
        }
    }
}
