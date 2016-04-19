namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Index
{
    /// <summary>
    ///     The family of Murmur hash functions provides a fast non-cryptograhpic hashing functions with a good distribution.
    /// </summary>
    /// <remarks>
    ///     <see cref="http://en.wikipedia.org/wiki/MurmurHash" />
    /// </remarks>
    public static unsafe class Murmur3
    {
        const uint Seed = 0xc58f1a7b;
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;

        public static uint Hash(string s)
        {
            fixed(char* input = s)
            {
                return Hash((byte*) input, (uint) s.Length*sizeof(char), Seed);
            }
        }

        public static uint Hash(byte[] data)
        {
            fixed(byte* input = &data[0])
            {
                return Hash(input, (uint) data.Length, Seed);
            }
        }

        static uint Hash(byte* data, uint len, uint seed)
        {
            var nblocks = len/4;
            var h1 = seed;

            //----------
            // body

            uint k1;
            var block = (uint*) data;
            for (var i = nblocks; i > 0; --i, ++block)
            {
                k1 = *block;

                k1 *= c1;
                k1 = Rotl32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = Rotl32(h1, 13);
                h1 = h1*5 + 0xe6546b64;
            }

            //----------
            // tail

            k1 = 0;
            var rem = len & 3;
            var tail = (byte*) block;
            if (rem >= 3)
            {
                k1 ^= (uint) (tail[2] << 16);
            }
            if (rem >= 2)
            {
                k1 ^= (uint) (tail[1] << 8);
            }
            if (rem > 0)
            {
                k1 ^= tail[0];
                k1 *= c1;
                k1 = Rotl32(k1, 15);
                k1 *= c2;
                h1 ^= k1;
            }

            //----------
            // finalization

            h1 ^= len;

            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6b;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35;
            h1 ^= h1 >> 16;

            return h1;
        }

        static uint Rotl32(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}