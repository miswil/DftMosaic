using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DftMosaic.Core.Mosaic.Files.Png
{
    internal class CRC32
    {
        private uint[] crcTable = new uint[256];

        public CRC32()
        {
            BuildCRC32Table();
        }

        private void BuildCRC32Table()
        {
            for (uint i = 0; i < 256; i++)
            {
                var x = i;
                for (var j = 0; j < 8; j++)
                {
                    x = (uint)((x & 1) == 0 ? x >> 1 : -306674912 ^ x >> 1);
                }
                this.crcTable[i] = x;
            }
        }

        public uint GetCRC32(byte[] buf)
        {
            uint num = uint.MaxValue;
            for (var i = 0; i < buf.Length; i++)
            {
                num = this.crcTable[(num ^ buf[i]) & 255] ^ num >> 8;
            }

            return (uint)(num ^ -1);
        }
    }
}
