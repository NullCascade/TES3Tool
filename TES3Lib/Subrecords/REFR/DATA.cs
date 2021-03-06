﻿using TES3Lib.Base;
using Utility;

namespace TES3Lib.Subrecords.REFR
{
    /// <summary>
    /// Position data
    /// </summary>
    public class DATA : Subrecord
    {
        public float XPos { get; set; }
        public float YPos { get; set; }
        public float ZPos { get; set; }
        public float XRotate { get; set; }
        public float YRotate { get; set; }
        public float ZRotate { get; set; }

        public DATA()
        {

        }

        public DATA(byte[] rawData) : base(rawData)
        {
            var reader = new ByteReader();
            XPos = reader.ReadBytes<float>(base.Data);
            YPos = reader.ReadBytes<float>(base.Data);
            ZPos = reader.ReadBytes<float>(base.Data);
            XRotate = reader.ReadBytes<float>(base.Data);
            YRotate = reader.ReadBytes<float>(base.Data);
            ZRotate = reader.ReadBytes<float>(base.Data);
        }
    }
}
