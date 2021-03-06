﻿using System;
using System.Linq;
using TES4Lib.Base;
using Utility;

namespace TES4Lib.Subrecords.REFR
{
    public class XTRG : Subrecord
    {
        /// <summary>
        /// Target Reference (REFR, ACHR or ACRE)
        /// </summary>
        public string TargetRefFormId { get; set; }

        public XTRG(byte[] rawData) : base(rawData)
        {
            var reader = new ByteReader();
            var targetRefFormIdBytes = reader.ReadBytes<byte[]>(base.Data, base.Size);
            TargetRefFormId = BitConverter.ToString(targetRefFormIdBytes.Reverse().ToArray()).Replace("-", "");

        }
    }
}
