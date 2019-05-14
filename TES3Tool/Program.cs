﻿using System;
using static TES4Lib.TES4;
using static TES3Lib.TES3;
using static TES3Tool.TES4RecordConverter.Oblivion2Morrowind;
using System.Collections.Generic;

namespace TES3Tool
{
    class Program
    {
        static void Main(string[] args)
        {
          // ConvertSI();

           //OblivionLoadTest();

            MWLoadTest();



            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void ConvertSI()
        {
            string fileESM = "D:\\Program Files\\Steam\\steamapps\\common\\Oblivion\\Data\\Oblivion.esm";

            var stat = TES4Load(fileESM, new List<string> {
                "STAT","WRLD","SOUN", "MISC","KEYM","FURN","ACTI","LIGH","CONT",
                "FLOR","WEAP","INGR","BOOK","ENCH","ALCH","AMMO","APPA", "ARMO",
                "CLOT","CREA","DOOR","FACT","CLAS","LVLC","LVLI","CELL","NPC_",
                "RACE",
            });
            var testEX = ConvertInteriorsAndExteriors(stat);
            testEX.TES3Save("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Morrowind\\Data Files\\SI.esp");
        }

        public static void MWLoadTest()
        {
            string fileESM = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Morrowind\\Data Files\\x.esp";

            var tes3 = TES3Load(fileESM, new List<string> { "RACE"});


            //foreach (var item in tes3.Records)
            //{
            //    if (item.Name == "BODY")
            //    {
            //        TES3Lib.Records.BODY body = item as TES3Lib.Records.BODY;
            //        if(body.FNAM != null &&
            //            body.BYDT.PartType == TES3Lib.Enums.BodyPartType.Skin &&
            //                body.BYDT.IsVampire == 0 &&
            //                    body.BYDT.BodyPart == TES3Lib.Enums.BodyPart.Head

                        
            //            )
            //        {
            //            var race = body.FNAM.Name;
            //            var gender = body.BYDT.Flags.Contains(TES3Lib.Enums.Flags.BodyPartFlag.Female) ? "F" : "M";
            //            var id = body.NAME.EditorId;
                        
            //            Console.WriteLine($"MWRaceFaces[\"{race}{gender}\"].Add(\"{id.TrimEnd('\0')}\\0\");");

            //        }
            //        //string race = body.


            //    }
            //}

            tes3.TES3Save("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Morrowind\\Data Files\\x2.esp");
        }

        public static void OblivionLoadTest()
        {
            string file = "D:\\Program Files\\Steam\\steamapps\\common\\Oblivion\\Data\\Oblivion.esm";
            var tes4 = TES4Load(file, new List<string> { "FACT","RACE" });
        }
    }
}
