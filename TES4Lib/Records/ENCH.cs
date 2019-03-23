﻿using System;
using System.Collections.Generic;
using TES4Lib.Base;
using TES4Lib.Subrecords.ENCH;
using TES4Lib.Subrecords.Shared;
using Utility;
using static Utility.Common;

namespace TES4Lib.Records
{
    public class ENCH : Record
    {
        public EDID EDID { get; set; }

        /// <summary>
        /// Might be not even needed
        /// </summary>
        public FULL FULL { get; set; }

        public ENIT ENIT { get; set; }

        public List<(EFID EFID, EFIT EFIT, SCIT SCIT, SULL FULL)> EFCT { get; set; }

        public ENCH(byte[] rawData) : base(rawData)
        {
            BuildSubrecords();
        }

        protected override void BuildSubrecords()
        {
            var readerData = new ByteReader();
            EFCT = new List<(EFID, EFIT, SCIT, SULL)>();
            while (Data.Length != readerData.offset)
            {
                var subrecordName = GetRecordName(readerData);
                var subrecordSize = GetRecordSize(readerData);

                if (subrecordName.Equals("EFID"))
                {
                    EFCT.Add((new EFID(readerData.ReadBytes<byte[]>(Data, (int)subrecordSize)), null, null, null));
                    continue;
                }

                if (subrecordName.Equals("EFIT"))
                {
                    int index = EFCT.Count - 1;
                    EFCT[index] = (EFCT[index].EFID, new EFIT(readerData.ReadBytes<byte[]>(Data, (int)subrecordSize)), EFCT[index].SCIT, EFCT[index].FULL);
                    continue;
                }

                if (subrecordName.Equals("SCIT"))
                {
                    int index = EFCT.Count - 1;
                    EFCT[index] = (EFCT[index].EFID, EFCT[index].EFIT, new SCIT(readerData.ReadBytes<byte[]>(Data, (int)subrecordSize)), EFCT[index].FULL);
                    continue;
                }

                if (subrecordName.Equals("FULL") && !IsNull(this.FULL))
                {
                    int index = EFCT.Count - 1;
                    EFCT[index] = (EFCT[index].EFID, EFCT[index].EFIT, EFCT[index].SCIT, new SULL(readerData.ReadBytes<byte[]>(Data, (int)subrecordSize)));
                    continue;
                }

                try
                {
                    var subrecordProp = this.GetType().GetProperty(subrecordName);
                    var subrecordData = readerData.ReadBytes<byte[]>(Data, (int)subrecordSize);

                    var subrecord = Activator.CreateInstance(subrecordProp.PropertyType, new object[] { subrecordData });
                    subrecordProp.SetValue(this, subrecord);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"error in building {this.GetType().ToString()} ar subrecord {subrecordName} eighter not implemented or borked {e}");
                    break;
                }
            }
        }
    }
}