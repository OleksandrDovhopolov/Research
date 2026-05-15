using System;
using System.Security.Cryptography;
using System.Text;

namespace TileEditor
{

    public static class UIDGenerator
    {
        //--------------------------------------------------------------------------------------------------------------------------

        private static readonly RNGCryptoServiceProvider _cryptoService = new RNGCryptoServiceProvider();

        private static readonly char[] _alphabet =
            "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private static readonly StringBuilder stringBuilder = new StringBuilder();

        private static byte[] _data;
        private static Random _seed;

        private static uint CalcHash32(string value) // Jenkins hash
        {
            var i = 0;
            var len = value.Length;
            var hash = 0u;
            while (i != len)
            {
                hash += value[i++];
                hash += hash << 10;
                hash ^= hash >> 10;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public static void SetSeed(string seed) => _seed = new Random(unchecked((int) CalcHash32(seed)));
        public static void SetSeed(int seed) => _seed = new Random(seed);
        public static void ClearSeed() => _seed = null;

        public static string Get(int size = 8)
        {
            if (_data == null || _data.Length != size) _data = new byte[size];

            if (_seed == null) _cryptoService.GetBytes(_data);
            else _seed.NextBytes(_data);

            var alphabetMaxIndex = _alphabet.Length - 1;

            stringBuilder.Clear();
            for (var i = 0; i < _data.Length; i++)
                stringBuilder.Append(_alphabet[_data[i] % alphabetMaxIndex]);
            return stringBuilder.ToString();
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }

}