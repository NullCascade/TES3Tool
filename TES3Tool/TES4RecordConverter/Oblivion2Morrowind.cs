﻿using System;
using System.Collections.Generic;
using System.Linq;
using static Utility.Common;
using static TES3Tool.TES4RecordConverter.Records.Helpers;
using static TES3Tool.TES4RecordConverter.Records.Converters;
using TES3Tool.TES4RecordConverter.Records;
using TES4Lib.Enums;
using TES4Lib.Base;

namespace TES3Tool.TES4RecordConverter
{
    public static class Oblivion2Morrowind
    {

        public static TES3Lib.TES3 ConvertInteriorCells(TES4Lib.TES4 tes4)
        {
            //convert cells
            var cellGroupsTop = tes4.Groups.FirstOrDefault(x => x.Label == "CELL");
            if (IsNull(cellGroupsTop))
            {
                Console.WriteLine("no CELL records");
                return null;
            }
            ConvertedRecords.Add("CELL", new List<ConvertedRecordData>());


            //this is soooo bad
            foreach (var cellBlock in cellGroupsTop.Groups)
            {
                foreach (var cellSubBlock in cellBlock.Groups)
                {
                    foreach (TES4Lib.Records.CELL cellRecord in cellSubBlock.Records)
                    {

                        if (cellRecord.Flag.Contains(TES4Lib.Enums.Flags.RecordFlag.Deleted)) continue;

                        //hack for now to get SI only
                        if ((cellRecord.EDID.EditorId.Contains("SE") || cellRecord.EDID.EditorId.Contains("XP")) && !IsNull(cellRecord.FULL))
                        {
                            var convertedCell = ConvertCELL(cellRecord);
                            if (IsNull(convertedCell)) throw new Exception("Output cell was null");

                            var cellReferences = cellSubBlock.Groups.FirstOrDefault(x => x.Label == cellRecord.FormId);
                            if (IsNull(cellReferences)) continue;

                            Console.WriteLine($"BEGIN CONVERTING \"{convertedCell.NAME.EditorId}\" CELL");
                            foreach (var childrenType in cellReferences.Groups) //can have 3 with labels: persistent 8; temporaty 9; distant 10;
                            {
                                int refrNumber = 1;
                                foreach (var obRef in childrenType.Records)
                                {
                                    if (obRef.Flag.Contains(TES4Lib.Enums.Flags.RecordFlag.Deleted)) continue;

                                    TES3Lib.Records.REFR mwREFR;

                                    var referenceTypeName = obRef.GetType().Name;

                                    if (referenceTypeName.Equals("REFR"))
                                    {
                                        var obREFR = (TES4Lib.Records.REFR)obRef;
                                        if (IsNull(obREFR.NAME)) continue;
                                        var ReferenceBaseFormId = obREFR.NAME.BaseFormId;

                                        //MOVE THIS TO SEPARATE FUNCTION
                                        var BaseId = GetBaseIdFromFormId(ReferenceBaseFormId);
                                        if (string.IsNullOrEmpty(BaseId))
                                        {
                                            var mwRecordFromREFR = ConvertRecordFromFormId(ReferenceBaseFormId);
                                            if (IsNull(mwRecordFromREFR)) continue;

                                            if (!ConvertedRecords.ContainsKey(mwRecordFromREFR.Type)) ConvertedRecords.Add(mwRecordFromREFR.Type, new List<ConvertedRecordData>());
                                            ConvertedRecords[mwRecordFromREFR.Type].Add(mwRecordFromREFR);

                                            BaseId = mwRecordFromREFR.EditorId;
                                        }
                                        /////////

                                        mwREFR = ConvertREFR(obREFR, BaseId, refrNumber);
                                        CellReferences.Add(new ConvertedCellReference(cellRecord.FormId, obREFR.FormId, mwREFR)); //for tracking

                                        convertedCell.REFR.Add(mwREFR);
                                        refrNumber++;
                                    }

                                    if (referenceTypeName.Equals("ACRE"))
                                    {
                                        var obACRE = (TES4Lib.Records.ACRE)obRef;
                                        if (IsNull(obACRE.NAME)) continue;
                                        var ReferenceBaseFormId = obACRE.NAME.BaseFormId;

                                        //MOVE THIS TO SEPARATE FUNCTION
                                        var BaseId = GetBaseIdFromFormId(ReferenceBaseFormId);
                                        if (string.IsNullOrEmpty(BaseId))
                                        {
                                            var mwRecordFromREFR = ConvertRecordFromFormId(ReferenceBaseFormId);
                                            if (IsNull(mwRecordFromREFR)) continue;

                                            if (!ConvertedRecords.ContainsKey(mwRecordFromREFR.Type)) ConvertedRecords.Add(mwRecordFromREFR.Type, new List<ConvertedRecordData>());
                                            ConvertedRecords[mwRecordFromREFR.Type].Add(mwRecordFromREFR);

                                            BaseId = mwRecordFromREFR.EditorId;
                                        }
                                        /////////

                                        mwREFR = ConvertACRE(obACRE, BaseId, refrNumber);
                                        CellReferences.Add(new ConvertedCellReference(cellRecord.FormId, obACRE.FormId, mwREFR)); //for tracking

                                        convertedCell.REFR.Add(mwREFR);
                                        refrNumber++;
                                    }

                                    if (referenceTypeName.Equals("ACHR"))
                                    {
                                        continue;
                                    }
                                }
                            }

                            foreach (var item in ConvertedRecords["CELL"])
                            {
                                bool cellWithSameNameExists = (item.Record as TES3Lib.Records.CELL).NAME.EditorId.Equals(convertedCell.NAME.EditorId);
                                if (cellWithSameNameExists)
                                {
                                    convertedCell.NAME.EditorId = CellNameFormatter($"{convertedCell.NAME.EditorId.Replace("\0", " ")}{cellRecord.EDID.EditorId}");
                                    break;
                                }
                            }

                            ConvertedRecords["CELL"].Add(new ConvertedRecordData(cellRecord.FormId, "CELL", cellRecord.EDID.EditorId, convertedCell));

                            Console.WriteLine($"DONE CONVERTING \"{convertedCell.NAME.EditorId}\" CELL");
                        }
                    }
                }
            }

            DoorDestinationsFormIdToNames();

            Console.WriteLine($"INTERIOR CELL AND REFERENCED RECORDS CONVERSION DONE \n BUILDING TES3 PLUGIN/MASTER INSTANCE");

            var tes3 = new TES3Lib.TES3();
            TES3Lib.Records.TES3 header = createTES3HEader();
            tes3.Records.Add(header);

            foreach (var record in Enum.GetNames(typeof(TES3Lib.RecordTypes)))
            {
                if (!ConvertedRecords.ContainsKey(record)) continue;
                tes3.Records.InsertRange(tes3.Records.Count, ConvertedRecords[record].Select(x => x.Record));
            }

            //dispose helper structures
            ConvertedRecords = new Dictionary<string, List<ConvertedRecordData>>();
            CellReferences = new List<ConvertedCellReference>();
            DoorDestinations = new List<TES3Lib.Subrecords.Shared.DNAM>();

            return tes3;
        }

        public static TES3Lib.TES3 ConvertExteriorObjects(TES4Lib.TES4 tes4)
        {
            //convert cells
            var wrldGroupsTop = tes4.Groups.FirstOrDefault(x => x.Label == "WRLD");
            if (IsNull(wrldGroupsTop))
            {
                Console.WriteLine("no WRLD records");
                return null;
            }
            ConvertedRecords.Add("CELL", new List<ConvertedRecordData>());

            foreach (var wrld in wrldGroupsTop.Records)
            {
                var wrldFormId = wrld.FormId;
                var worldChildren = wrldGroupsTop.Groups.FirstOrDefault(x => x.Label == wrldFormId);

                if (IsNull(worldChildren))
                {
                    Console.WriteLine("WRLD has no WorldChildren");
                    continue;
                }

                //Here are records ROAD, CELL but i dont know if i need them, so i just proceed to exterior subblocks

                foreach (var exteriorCellBlock in worldChildren.Groups)
                {
                    if (exteriorCellBlock.Type.Equals(GroupLabel.CellChildren)) continue; // that might happen but skip for now

                    ProcessExteriorSubBlocks(exteriorCellBlock);
                }

            }

            var tes3 = new TES3Lib.TES3();
            TES3Lib.Records.TES3 header = createTES3HEader();
            tes3.Records.Add(header);

            foreach (var record in Enum.GetNames(typeof(TES3Lib.RecordTypes)))
            {
                if (!ConvertedRecords.ContainsKey(record)) continue;
                tes3.Records.InsertRange(tes3.Records.Count, ConvertedRecords[record].Select(x => x.Record));
            }

            //dispose helper structures
            ConvertedRecords = new Dictionary<string, List<ConvertedRecordData>>();
            CellReferences = new List<ConvertedCellReference>();
            DoorDestinations = new List<TES3Lib.Subrecords.Shared.DNAM>();

            return tes3;
        }

        static void ProcessExteriorSubBlocks(Group exteriorCellBlock)
        {
            foreach (var exteriorCell in exteriorCellBlock.Records)
            {
                //convert cell
                var cellChildren = exteriorCellBlock.Groups.FirstOrDefault(x => x.Label == exteriorCell.FormId);

                if (IsNull(cellChildren))
                {
                    Console.WriteLine("cell has no objects");
                    continue;
                }

                //CONVEEEERT
                foreach (var childrenType in cellChildren.Groups)
                {
                    //TODO: start here tomorrow
                }
            }
        }

        private static TES3Lib.Records.TES3 createTES3HEader()
        {
            var header = new TES3Lib.Records.TES3
            {
                HEDR = new TES3Lib.Subrecords.TES3.HEDR
                {
                    CompanyName = "TES3Tool\0",
                    Description = "\0",
                    NumRecords = 666,
                    ESMFlag = 0,
                    Version = 1.3f,
                },
                MAST = new TES3Lib.Subrecords.TES3.MAST
                {
                    Filename = "Morrowind.esm\0",
                },
                DATA = new TES3Lib.Subrecords.TES3.DATA
                {
                    MasterDataSize = 6666 //should not break but fix that later
                }
            };
            return header;
        }

        public static ConvertedRecordData ConvertRecordFromFormId(string BaseFormId)
        {
            TES4Lib.Base.Record record;
            TES4Lib.TES4.TES4RecordIndex.TryGetValue(BaseFormId, out record);
            if (IsNull(record)) return null;

            var mwRecordFromREFR = ConvertRecord(record);

            return mwRecordFromREFR;
        }
    }
}
