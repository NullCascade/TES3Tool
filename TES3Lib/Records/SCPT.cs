﻿using TES3Lib.Base;
using TES3Lib.Subrecords.SCPT;

namespace TES3Lib.Records
{
    /// <summary>
    /// Script Record
    /// </summary>
    public class SCPT: Record
    {
        public SCHD SCHD { get; set; }

        public SCVR SCVR { get; set; }

        public SCDT SCDT { get; set; }

        public SCTX SCTX { get; set; }

        public SCPT()
        {

        }

        public SCPT(byte[] rawData) : base(rawData)
        {
            BuildSubrecords();
        }
    }
}
