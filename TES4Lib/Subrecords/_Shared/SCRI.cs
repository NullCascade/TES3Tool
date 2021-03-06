﻿using System;
using System.Linq;
using TES4Lib.Base;
using Utility;

namespace TES4Lib.Subrecords.Shared
{
    /// <summary>
    /// Script attached to record
    /// </summary>
    public class SCRI : Subrecord
    {
        /// <summary>
        /// Script formId
        /// </summary>
        public string ScriptFormId { get; set; }

        public SCRI(byte[] rawData) : base(rawData)
        {
            var reader = new ByteReader();
            var baseFormIdBytes = reader.ReadBytes<byte[]>(base.Data, base.Size);
            ScriptFormId = BitConverter.ToString(baseFormIdBytes.Reverse().ToArray()).Replace("-", "");
        }
    }
}