﻿using TES4Lib.Base;
using Utility;

namespace TES4Lib.Subrecords.CLOT
{
    /// <summary>
    /// Cloth data value/weight
    /// </summary>
    public class DATA : Subrecord
    {
        public int Value { get; set; }

        public float Weight { get; set; }

        public DATA(byte[] rawData) : base(rawData)
        {
            var reader = new ByteReader();
            Value = reader.ReadBytes<int>(base.Data);
            Weight = reader.ReadBytes<float>(base.Data);
        }
    }
}
