﻿using System;
using System.Collections.Generic;
using System.Linq;
using static Utility.Common;
using static TES3Oblivion.Helpers;
using static TES3Oblivion.RecordConverter;
using static TES3Oblivion.SIPostProcessing.Definitions.BodyParts;
using TES3Oblivion.SIPostProcessing.Definitions;
using TES4Lib.Base;
using TES4Lib.Enums;
using System.Threading.Tasks;
using TES3Oblivion.Records.SIPostProcessing.Definitions;
using TES3Oblivion.SIPostProcessing;

namespace TES3Oblivion
{
    public static class CellConverter
    {
        public static TES3Lib.TES3 ConvertInteriorsAndExteriors(TES4Lib.TES4 tes4)
        {
            ConvertRecords(tes4, "SE");

            ConvertedRecords.Add("CELL", new List<ConvertedRecordData>());
            ConvertedRecords.Add("PGRD", new List<ConvertedRecordData>());

            ConvertInteriorCells(tes4);
            ConvertExteriorCells(tes4);

            UpdateDoorReferences();

            //SI
            PostProcessing();

            var tes3 = new TES3Lib.TES3();
            TES3Lib.Records.TES3 header = createTES3HEader();
            tes3.Records.Add(header);

            EquipementSplitter.SELL0NPCOrderKnightArmor100();

            foreach (var record in Enum.GetNames(typeof(TES3Lib.RecordTypes)))
            {
                //SI
                if (record.Equals("BODY"))
                {
                    tes3.Records.AddRange(GetListOfBodyParts());
                }

                if (!ConvertedRecords.ContainsKey(record)) continue;
                tes3.Records.AddRange(ConvertedRecords[record].Select(x => x.Record));
            }

            //dispose helper structures
            ConvertedRecords = new Dictionary<string, List<ConvertedRecordData>>();
            CellReferences = new List<ConvertedCellReference>();
            DoorReferences = new List<TES3Lib.Records.REFR>();

            var dupex = tes3.Records.Find(x => x.GetEditorId() == "SEOrderKnightArmor1Iron\0");

            return tes3;
        }

        public static TES3Lib.TES3 ConvertInteriors(TES4Lib.TES4 tes4)
        {
            ConvertedRecords.Add("CELL", new List<ConvertedRecordData>());

            ConvertInteriorCells(tes4);

            UpdateDoorReferences();

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
            DoorReferences = new List<TES3Lib.Records.REFR>();

            return tes3;
        }

        public static TES3Lib.TES3 ConvertExteriors(TES4Lib.TES4 tes4)
        {
            ConvertedRecords.Add("CELL", new List<ConvertedRecordData>());

            ConvertExteriorCells(tes4);

            Console.WriteLine($"EXTERIOR CELL AND REFERENCED RECORDS CONVERSION DONE \n BUILDING TES3 PLUGIN/MASTER INSTANCE");

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
            DoorReferences = new List<TES3Lib.Records.REFR>();

            return tes3;
        }

        private static void ConvertInteriorCells(TES4Lib.TES4 tes4)
        {
            //convert cells
            var cellGroupsTop = tes4.Groups.FirstOrDefault(x => x.Label == "CELL");
            if (IsNull(cellGroupsTop))
            {
                Console.WriteLine("no CELL records");
                return;
            }

            foreach (var cellBlock in cellGroupsTop.Groups)
            {
                ProcessInteriorSubBlocks(cellBlock);
            }
        }

        private static void ConvertExteriorCells(TES4Lib.TES4 tes4)
        {
            //convert cells
            var wrldGroupsTop = tes4.Groups.FirstOrDefault(x => x.Label == "WRLD");
            if (IsNull(wrldGroupsTop))
            {
                Console.WriteLine("no WRLD records");
                return;
            }

            foreach (TES4Lib.Records.WRLD wrld in wrldGroupsTop.Records)
            {
                Console.WriteLine($"Converting worldspace {wrld.FULL.DisplayName}");

                var wrldFormId = wrld.FormId;
                var worldChildren = wrldGroupsTop.Groups.FirstOrDefault(x => x.Label == wrldFormId);

                if (IsNull(worldChildren))
                {
                    Console.WriteLine($"{wrld.FULL.DisplayName} has no WorldChildren");
                    continue;
                }

                //Here are records ROAD

                foreach (var exteriorCellBlock in worldChildren.Groups)
                {
                    if (exteriorCellBlock.Type.Equals(GroupLabel.CellChildren)) continue;

                    ProcessExteriorSubBlocks(exteriorCellBlock);
                }

                //process extra CELL record
                var wrldCell = worldChildren.Records.FirstOrDefault(x => x.Name.Equals("CELL")) as TES4Lib.Records.CELL;

                if (!IsNull(wrldCell))
                {
                   

                    //gardens hacks
                    if (wrld.EDID.EditorId.Equals("SEManiaGarden\0") || wrld.EDID.EditorId.Equals("SEDementiaGarden\0"))
                    {
                        wrldCell.DATA.Flags.Add(TES4Lib.Enums.Flags.CellFlag.IsInteriorCell);
                        wrldCell.DATA.Flags.Add(TES4Lib.Enums.Flags.CellFlag.BehaveLikeExterior);

                        var convertedGardenCell = ConvertCELL(wrldCell);

                        var gWrldCellChildren = worldChildren.Groups.FirstOrDefault(x => x.Type.Equals(GroupLabel.CellChildren));
                        ConvertCellChildren(ref convertedGardenCell, gWrldCellChildren, wrld.FormId);

                        DistributeGardenReferences(convertedGardenCell, wrld.EDID.EditorId);
                        continue;
                    }

                    var convertedCell = ConvertCELL(wrldCell);
                    var wrldCellChildren = worldChildren.Groups.FirstOrDefault(x => x.Type.Equals(GroupLabel.CellChildren));
                    ConvertCellChildren(ref convertedCell, wrldCellChildren, wrld.FormId);
                    DistributeWorldSpaceReferecnes(convertedCell);
                }

                //merge path grids: mabye some other time
                //foreach (var exteriorCellPathGrid in ExteriorPathGrids)
                //{
                //    var mergedGrid = MergeExteriorPathGrids(exteriorCellPathGrid.Value);
                //    ConvertedRecords["PGRD"].Add(new ConvertedRecordData("PGRD", "PGRD", "PGRD", mergedGrid));
                //}

            }
        }

        private static void DistributeGardenReferences(TES3Lib.Records.CELL convertedCell, string gardenWorldName)
        {

            string cellName = gardenWorldName == "SEManiaGarden\0" ? "SEManiaGardenExterior\0" : "SEDementiaGardenExterior\0";
            ConvertedRecordData targetConvertedCell = ConvertedRecords["CELL"].FirstOrDefault(x =>
                  (x.Record as TES3Lib.Records.CELL).NAME.EditorId.Equals(cellName));

            foreach (var cellReference in convertedCell.REFR)
            {
                if (!IsNull(targetConvertedCell))
                {
                    var cellRecord = targetConvertedCell.Record as TES3Lib.Records.CELL;
                    cellRecord.NAM0.ReferenceCount++;
                    cellReference.FRMR.ObjectIndex = cellRecord.NAM0.ReferenceCount;
                    cellRecord.REFR.Add(cellReference);

                    //need update parent formId
                    var convertedReference = CellReferences.FirstOrDefault(x => x.Reference.Equals(cellReference));
                    convertedReference.ParentCellFormId = targetConvertedCell.OriginFormId;
                }
            }
        }

        private static void DistributeWorldSpaceReferecnes(TES3Lib.Records.CELL convertedCell)
        {
            foreach (var cellReference in convertedCell.REFR)
            {
                int cellGrindX = (int)(cellReference.DATA.XPos / Config.mwCellSize);
                int cellGrindY = (int)(cellReference.DATA.YPos / Config.mwCellSize);

                ConvertedRecordData targetConvertedCell = ConvertedRecords["CELL"].FirstOrDefault(x =>
                    (x.Record as TES3Lib.Records.CELL).DATA.GridX.Equals(cellGrindX) && (x.Record as TES3Lib.Records.CELL).DATA.GridY.Equals(cellGrindY));

                if (!IsNull(targetConvertedCell))
                {
                    var cellRecord = targetConvertedCell.Record as TES3Lib.Records.CELL;
                    cellRecord.NAM0.ReferenceCount++;
                    cellReference.FRMR.ObjectIndex = cellRecord.NAM0.ReferenceCount;
                    cellRecord.REFR.Add(cellReference);

                    //need update parent formId
                    var convertedReference = CellReferences.FirstOrDefault(x => x.Reference.Equals(cellReference));
                    convertedReference.ParentCellFormId = targetConvertedCell.OriginFormId;

                }
                else
                {
                    Console.WriteLine($"target cell at coordinates {cellGrindX}.{cellGrindY} not found");
                }
            }
        }

        private static TES3Lib.Records.PGRD MergeExteriorPathGrids(List<ConvertedExteriorPathgrid> pathGrids)
        {
            var mergedPGRD = new TES3Lib.Records.PGRD() { DATA = new TES3Lib.Subrecords.PGRD.DATA() };
            mergedPGRD.DATA.Granularity = pathGrids[0].PathGrid.DATA.Granularity;
            mergedPGRD.DATA.GridX = pathGrids[0].PathGrid.DATA.GridX;
            mergedPGRD.DATA.GridY = pathGrids[0].PathGrid.DATA.GridY;


            var points = new List<TES3Lib.Subrecords.PGRD.PGRP.Point>();

            var edges = new List<int>();
            int offset = 0;

            foreach (var subGrid in pathGrids)
            {
                if (IsNull(subGrid.PathGrid.PGRC)) continue;

                offset = points.Count;
                points.AddRange(subGrid.PathGrid.PGRP.Points);
                edges.AddRange(Array.ConvertAll(subGrid.PathGrid.PGRC.Edges, edge => edge + offset).ToList());
            }


            mergedPGRD.PGRP = new TES3Lib.Subrecords.PGRD.PGRP { Points = points.ToArray() };
            mergedPGRD.PGRC = new TES3Lib.Subrecords.PGRD.PGRC { Edges = edges.ToArray() };
            mergedPGRD.DATA.Points = (short)points.Count;

            var cell = ConvertedRecords["CELL"].FirstOrDefault( x => 
            (x.Record as TES3Lib.Records.CELL).DATA.GridX.Equals(mergedPGRD.DATA.GridX) &&
            (x.Record as TES3Lib.Records.CELL).DATA.GridY.Equals(mergedPGRD.DATA.GridY)).Record as TES3Lib.Records.CELL;

            mergedPGRD.NAME = new TES3Lib.Subrecords.Shared.NAME();

            if (!IsNull(cell.NAME) && !cell.NAME.EditorId.Equals("\0"))
            {
                mergedPGRD.NAME.EditorId = cell.NAME.EditorId;
            }
            else if (!IsNull(cell.RGNN))
            {
                mergedPGRD.NAME.EditorId = cell.RGNN.RegionName;
            }
            else
            {
                mergedPGRD.NAME.EditorId = "Wilderness\0";
            }

            return mergedPGRD;
        }

        private static void ProcessInteriorSubBlocks(Group interiorSubBlock)
        {
            foreach (var cellSubBlock in interiorSubBlock.Groups)
            {
                foreach (TES4Lib.Records.CELL interiorCell in cellSubBlock.Records)
                {
                    string cellFormId = interiorCell.FormId;

                    if (interiorCell.Flag.Contains(TES4Lib.Enums.Flags.RecordFlag.Deleted)) continue;

                    //hack for now to get SI only
                    if ((interiorCell.EDID.EditorId.Contains("SE") || interiorCell.EDID.EditorId.Contains("XP")) && !IsNull(interiorCell.FULL))
                    {
                        var convertedCell = ConvertCELL(interiorCell);
                        if (IsNull(convertedCell)) throw new Exception("Output cell was null");

                        var cellChildren = cellSubBlock.Groups.FirstOrDefault(x => x.Label == interiorCell.FormId);
                        if (IsNull(cellChildren)) continue;

                        Console.WriteLine($"BEGIN CONVERTING \"{convertedCell.NAME.EditorId}\" CELL");

                        ConvertCellChildren(ref convertedCell, cellChildren, cellFormId);

                        foreach (var item in ConvertedRecords["CELL"])
                        {
                            bool cellWithSameNameExists = (item.Record as TES3Lib.Records.CELL).NAME.EditorId.Equals(convertedCell.NAME.EditorId);
                            if (cellWithSameNameExists)
                            {
                                convertedCell.NAME.EditorId = CellNameFormatter($"{convertedCell.NAME.EditorId.Replace("\0", " ")}{interiorCell.EDID.EditorId}");
                                break;
                            }
                        }

                        ConvertedRecords["CELL"].Add(new ConvertedRecordData(interiorCell.FormId, "CELL", interiorCell.EDID.EditorId, convertedCell));

                        Console.WriteLine($"DONE CONVERTING \"{convertedCell.NAME.EditorId}\" CELL");
                    }
                }
            }
        }

        private static void ProcessExteriorSubBlocks(Group exteriorCellBlock)
        {
            foreach (var subBlocks in exteriorCellBlock.Groups)
            {
                foreach (TES4Lib.Records.CELL exteriorCell in subBlocks.Records)
                {
                    bool cellMerge = false;
                    string cellFormId = exteriorCell.FormId;

                    //mania/dementia garden hacks
                    if (!IsNull(exteriorCell.EDID) && (exteriorCell.EDID.EditorId.Equals("SEDementiaGardenExterior\0") || exteriorCell.EDID.EditorId.Equals("SEManiaGardenExterior\0")))
                    {
                        exteriorCell.DATA.Flags.Add(TES4Lib.Enums.Flags.CellFlag.IsInteriorCell);
                        exteriorCell.DATA.Flags.Add(TES4Lib.Enums.Flags.CellFlag.BehaveLikeExterior);

                        var gardenCell = ConvertCELL(exteriorCell);
                        var gardenChildren = subBlocks.Groups.FirstOrDefault(x => x.Label == exteriorCell.FormId);
                        ConvertCellChildren(ref gardenCell, gardenChildren, cellFormId);
                        ConvertedRecords["CELL"].Add(new ConvertedRecordData(exteriorCell.FormId, "CELL", exteriorCell.EDID.EditorId, gardenCell));
                        return;
                    }


                    var convertedCell = ConvertCELL(exteriorCell);

                    // resolve if this cell at this grid already exist
                    foreach (var alreadyConvertedCell in ConvertedRecords["CELL"])
                    {
                        if (convertedCell.Equals(alreadyConvertedCell.Record as TES3Lib.Records.CELL))
                        {
                            cellMerge = true;
                            cellFormId = alreadyConvertedCell.OriginFormId;

                            alreadyConvertedCell.OriginFormId += exteriorCell.FormId;  //store all source cells formid as one string

                            convertedCell = mergeExteriorCells(alreadyConvertedCell.Record as TES3Lib.Records.CELL, convertedCell);

                            Console.WriteLine("merging subcells...");
                            break;
                        }
                    }

                    var cellChildren = subBlocks.Groups.FirstOrDefault(x => x.Label == exteriorCell.FormId);

                    if (IsNull(cellChildren))
                    {
                        Console.WriteLine("cell has no objects");
                        continue;
                    }

                    ConvertCellChildren(ref convertedCell, cellChildren, cellFormId);

                    if (!cellMerge)
                    {
                        //if (IsNull(convertedCell.RGNN) && convertedCell.REFR.Count.Equals(0)) return;

                        var editorId = !IsNull(exteriorCell.EDID) ? exteriorCell.EDID.EditorId : $"{exteriorCell.XCLC.GridX},{exteriorCell.XCLC.GridY}";
                        ConvertedRecords["CELL"].Add(new ConvertedRecordData(exteriorCell.FormId, "CELL", editorId, convertedCell));
                    }
                    Console.WriteLine($"DONE CONVERTING \"{convertedCell.DATA.GridX},{convertedCell.DATA.GridY}\" CELL");
                }
            }
        }

        private static void ConvertCellChildren(ref TES3Lib.Records.CELL mwCELL, Group cellChildren, string originalCellFormId)
        {
            foreach (var childrenType in cellChildren.Groups)
            {
                if (IsNull(mwCELL.NAM0))
                {
                    mwCELL.NAM0 = new TES3Lib.Subrecords.CELL.NAM0 { ReferenceCount = 1 };
                }

                foreach (var obRef in childrenType.Records)
                {
                    if (obRef.Flag.Contains(TES4Lib.Enums.Flags.RecordFlag.Deleted)) continue;

                    TES3Lib.Records.REFR mwREFR;

                    var referenceTypeName = obRef.GetType().Name;

                    if (referenceTypeName.Equals("REFR"))
                    {
                        var obREFR = obRef as TES4Lib.Records.REFR;
                        if (IsNull(obREFR.NAME)) continue;
                        var ReferenceBaseFormId = obREFR.NAME.BaseFormId;

                        var BaseId = GetBaseId(ReferenceBaseFormId);
                        if (string.IsNullOrEmpty(BaseId)) continue;

                        mwREFR = ConvertREFR(obREFR, BaseId, mwCELL.NAM0.ReferenceCount, mwCELL.DATA.Flags.Contains(TES3Lib.Enums.Flags.CellFlag.IsInteriorCell));
                        CellReferences.Add(new ConvertedCellReference(originalCellFormId, obREFR.FormId, mwREFR)); //for tracking

                        {// disable exterior statics as requested, remove when SI mod will be finished
                            if (!mwCELL.DATA.Flags.Contains(TES3Lib.Enums.Flags.CellFlag.IsInteriorCell))
                                if (ConvertedRecords.ContainsKey("STAT") && ConvertedRecords["STAT"].Any(x => x.EditorId.Equals(BaseId)))
                                    continue;
                        }               

                        mwCELL.REFR.Add(mwREFR);
                        mwCELL.NAM0.ReferenceCount++;
                        continue;
                    }

                    if (referenceTypeName.Equals("ACRE"))
                    {
                        var obACRE = obRef as TES4Lib.Records.ACRE;
                        if (IsNull(obACRE.NAME)) continue;
                        var ReferenceBaseFormId = obACRE.NAME.BaseFormId;

                        var BaseId = GetBaseId(ReferenceBaseFormId);
                        if (string.IsNullOrEmpty(BaseId)) continue;

                        mwREFR = ConvertACRE(obACRE, BaseId, mwCELL.NAM0.ReferenceCount, mwCELL.DATA.Flags.Contains(TES3Lib.Enums.Flags.CellFlag.IsInteriorCell));
                        CellReferences.Add(new ConvertedCellReference(originalCellFormId, obACRE.FormId, mwREFR)); //for tracking

                        mwCELL.REFR.Add(mwREFR);
                        mwCELL.NAM0.ReferenceCount++;
                        continue;
                    }

                    if (referenceTypeName.Equals("ACHR"))
                    {
                        var obACHR = obRef as TES4Lib.Records.ACHR;
                        if (IsNull(obACHR.NAME)) continue;
                        var ReferenceBaseFormId = obACHR.NAME.BaseFormId;

                        var BaseId = GetBaseId(ReferenceBaseFormId);
                        if (string.IsNullOrEmpty(BaseId)) continue;

                        mwREFR = ConvertACHR(obACHR, BaseId, mwCELL.NAM0.ReferenceCount, mwCELL.DATA.Flags.Contains(TES3Lib.Enums.Flags.CellFlag.IsInteriorCell));
                        CellReferences.Add(new ConvertedCellReference(originalCellFormId, obACHR.FormId, mwREFR)); //for tracking

                        mwCELL.REFR.Add(mwREFR);
                        mwCELL.NAM0.ReferenceCount++;
                        continue;
                    }

                    if (referenceTypeName.Equals("LAND"))
                    {
                        continue;
                    }

                    if (referenceTypeName.Equals("PGRD"))
                    {
                        if (mwCELL.DATA.Flags.Contains(TES3Lib.Enums.Flags.CellFlag.IsInteriorCell))
                        {
                            var obPGRD = obRef as TES4Lib.Records.PGRD;
                            var mwPGRD = ConvertPGRD(obPGRD, mwCELL);
                            ConvertedRecords["PGRD"].Add(new ConvertedRecordData(originalCellFormId, "CELL", mwCELL.NAME.EditorId, mwPGRD));
                            continue;
                        }
                        //else
                        //{
                        //    var coordinates = $"{mwPGRD.DATA.GridX},{mwPGRD.DATA.GridY}";

                        //    if (!ExteriorPathGrids.ContainsKey(coordinates))
                        //    {
                        //        ExteriorPathGrids.Add(coordinates, new List<ConvertedExteriorPathgrid>());
                        //    }

                        //    ExteriorPathGrids[coordinates].Add(new ConvertedExteriorPathgrid(mwPGRD, obPGRD.PGRI));
                        //    continue;
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// Convert all non hierarchical records from loaded TES4 file
        /// </summary>
        /// <param name="tes4">TES4 ESM/ESP with records</param>
        /// <param name="prefix">optional prefix for records editorId to convert</param>
        private static void ConvertRecords(TES4Lib.TES4 tes4, string prefix = null)
        {
            foreach (TES4Lib.Base.Group group in tes4.Groups)
            {
                if (group.Label.Equals("CELL") || group.Label.Equals("DIAL") || group.Label.Equals("WRLD")) continue;

                foreach (TES4Lib.Base.Record record in group.Records)
                {
                    string editorId = record.GetEditorId();
                    if (string.IsNullOrEmpty(editorId) || editorId.Equals("SE14GSEscortList\0")) continue;

                    if (!string.IsNullOrEmpty(prefix))
                    {

                        if (editorId.StartsWith(prefix) || editorId.StartsWith("XP"))
                        {
                            ConvertedRecordData mwRecord = ConvertRecord(record);
                            if (IsNull(mwRecord)) continue;
                            if (!ConvertedRecords.ContainsKey(mwRecord.Type)) ConvertedRecords.Add(mwRecord.Type, new List<ConvertedRecordData>());
                            ConvertedRecords[mwRecord.Type].Add(mwRecord);                       
                        }
                    }
                    else
                    {
                        ConvertedRecordData mwRecord = ConvertRecord(record);
                        if (!ConvertedRecords.ContainsKey(mwRecord.Type)) ConvertedRecords.Add(mwRecord.Type, new List<ConvertedRecordData>());
                        ConvertedRecords[mwRecord.Type].Add(mwRecord);
                    }              
                }
            }
        }

        private static void PostProcessing()
        {
            //1 split TODO
            
            Parallel.ForEach(ConvertedRecords["CLOT"], item => {
                if (EquipementProcessMap.ProcessItem.ContainsKey(item.EditorId))
                {
                    EquipementProcessMap.ProcessItem[item.EditorId].Invoke(item.Record as TES3Lib.Base.IEquipement);
                }
            });

            //Parallel.ForEach(ConvertedRecords["ARMO"], item => {
            //    if (EquipementItemsMap.ProcessItem.ContainsKey(item.EditorId))
            //    {
            //        EquipementItemsMap.ProcessItem[item.EditorId].Invoke(item.Record as TES3Lib.Base.IEquipement);
            //    }
            //});
        }

        private static TES3Lib.Records.CELL mergeExteriorCells(TES3Lib.Records.CELL cellBase, TES3Lib.Records.CELL cellToMerge)
        {
            cellBase.NAME = cellBase.NAME.EditorId.Equals("\0") ? cellToMerge.NAME : cellBase.NAME;
            cellBase.RGNN = IsNull(cellBase.RGNN) ? cellToMerge.RGNN : cellBase.RGNN;

            return cellBase;
        }
    }
}
