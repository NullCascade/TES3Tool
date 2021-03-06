﻿using System.Collections.Generic;
using TES4Lib.Base;
using TES4Lib.Enums;
using TES4Lib.Enums.Flags;
using Utility;

namespace TES4Lib.Subrecords.Shared
{
    /// <summary>
    /// Armor flags and body slot info
    /// </summary>
    public class BMDT : Subrecord
    {
        public HashSet<BodySlot> BodySlots { get; set; }

        public HashSet<EquipmentFlag> Flags { get; set; }

        public BMDT(byte[] rawData) : base(rawData)
        {
            var reader = new ByteReader();
            BodySlots = reader.ReadFlagBytes<BodySlot>(base.Data);
            Flags = reader.ReadFlagBytes<EquipmentFlag>(base.Data);
        }
    }
}
