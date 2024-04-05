using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunnyAnticheat
{
    class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Incorrect number of arguments. Run with \"--help\" for usage information.");
                return 1;
            }
            string ifile = args[0];

            if (ifile == "--help")
            {
                String[] helpText = {
                    "Usage:",
                    "gbafe_anticheat.exe input_filepath [freespace_offset]",
                    "input_filepath - Path to input ROM file.",
                    "freespace_offset - Optional offset to use for anticheat. Defaults to 0x1F00000."
                };
                foreach (string line in helpText) {
                    Console.WriteLine(line);
                }
                return 1;
            }

            uint freespaceOffset = 0x1F00000;
            if (args.Length > 1) freespaceOffset = uint.Parse(args[1]);

            //load input file into memory
            byte[] rom = File.ReadAllBytes(ifile);
            Array.Resize(ref rom, 0x2000000);

            //load table pointers from vanilla positions, then replace them with 0
            uint chapterTable = ReadWord(rom, 0x3462C);
            //uint textTable = ReadWord(rom, 0xA26C);

            //install anticheat functions in freespace; record effective locations of each
            byte[] func1 = CollateFunction(Function1BytecodeA, Function1BytecodeB, chapterTable);
            //byte[] func2 = CollateFunction(Function2BytecodeA, Function2BytecodeB, textTable);
            //byte[] func3 = CollateFunction(Function3BytecodeA, Function3BytecodeB, textTable);

            uint func1loc = freespaceOffset;
            //uint func2loc = (uint)(freespaceOffset + func1.Length);
            //uint func3loc = (uint)(freespaceOffset + func1.Length + func2.Length);

            MergeArraysAtOffset(rom, func1, func1loc);
            //MergeArraysAtOffset(rom, func2, func2loc);
            //MergeArraysAtOffset(rom, func3, func3loc);

            //apply anticheat hooks at table pointer load locations
            byte[] jth1 = CollateHook(jumpToHackBytecode, func1loc | 0x08000001);
            //byte[] jth2 = CollateHook(jumpToHackBytecode, func2loc | 0x08000001);
            //byte[] jth3 = CollateHook(jumpToHackBytecode, func1loc | 0x08000001);

            uint jth1loc = 0x34618;
            //uint jth2loc = 0xA240;
            //uint jth3loc = 0xB510;

            MergeArraysAtOffset(rom, jth1, jth1loc);
            //MergeArraysAtOffset(rom, jth2, jth2loc);
            //MergeArraysAtOffset(rom, jth3, jth3loc);

            //get rid of original pointers
            WriteWord(rom, 0x3462C, 0);
            WriteWord(rom, 0xB5E68, 0);
            WriteWord(rom, 0xB5F98, 0);
            WriteWord(rom, 0xB61C0, 0);
            WriteWord(rom, 0xB6328, 0);
            WriteWord(rom, 0xB6500, 0);
            //WriteWord(rom, 0xA26C, 0);
            //WriteWord(rom, 0xA2A0, 0);

            //save edited file back to input file
            File.WriteAllBytes(ifile, rom);

            return 0;
        }

        private static uint ReadWord(byte[] rom, uint offset)
        {
            uint i = 0;

            i += rom[offset];
            i += (uint)(rom[offset + 1] << 8);
            i += (uint)(rom[offset + 2] << 16);
            i += (uint)(rom[offset + 3] << 24);

            return i;
        }

        private static void WriteWord(byte[] rom, int offset, uint word)
        {
            byte i1 = (byte)(word & 0xFF);
            byte i2 = (byte)((word & 0xFF00) >> 8);
            byte i3 = (byte)((word & 0xFF0000) >> 16);
            byte i4 = (byte)((word & 0xFF000000) >> 24);

            rom[offset] = i1;
            rom[offset + 1] = i2;
            rom[offset + 2] = i3;
            rom[offset + 3] = i4;

        }

        private static byte[] CollateFunction(byte[] funcA, byte[] funcB, uint pointer)
        {
            ArrayList i = new ArrayList(); 
            i.AddRange(funcA);

            byte i1 = (byte)(pointer & 0xFF);
            byte i2 = (byte)((pointer & 0xFF00) >> 8);
            byte i3 = (byte)((pointer & 0xFF0000) >> 16);
            byte i4 = (byte)((pointer & 0xFF000000) >> 24);

            i.Add(i1);
            i.Add(i2);
            i.Add(i3);
            i.Add(i4);

            i.AddRange(funcB);

            return (byte[])i.ToArray(typeof(byte));

        }

        private static byte[] CollateHook(byte[] hook, uint pointer)
        {
            ArrayList i = new ArrayList();
            i.AddRange(hook); 

            byte i1 = (byte)(pointer & 0xFF);
            byte i2 = (byte)((pointer & 0xFF00) >> 8);
            byte i3 = (byte)((pointer & 0xFF0000) >> 16);
            byte i4 = (byte)((pointer & 0xFF000000) >> 24);

            i.Add(i1);
            i.Add(i2);
            i.Add(i3);
            i.Add(i4);

            return (byte[])i.ToArray(typeof(byte));

        }



        private static byte[] MergeArraysAtOffset(byte[] rom, byte[] function, uint offset)
        {
            for (int i = 0; i < function.Length; i++)
            {
                rom[offset + i] = function[i];
            }

            return rom;
        }

        private static byte[] Function1BytecodeA = {
            0x00, 0xB5, 0x01, 0x1C, 0x7F, 0x29, 0x07, 0xD0, 0x94, 0x20, 0x48, 0x43, 0x01, 0x49, 0x40, 0x18, 0x05, 0xE0, 0x00, 0x00
        };

        private static byte[] Function1BytecodeB = {
            0x02, 0x48, 0x00, 0x68, 0x00, 0x68, 0x02, 0xBC, 0x08, 0x47, 0x00, 0x00, 0x34, 0xFB, 0xA1, 0x08
        };

        private static byte[] Function2BytecodeA = {
            0x70, 0xB5, 0x05, 0x1C, 0x0B, 0x4E, 0x30, 0x68, 0x85, 0x42, 0x0F, 0xD0, 0x0A, 0x49, 0xA8, 0x00, 0x40, 0x18, 0x00, 0x68, 0x09, 0x4C, 0x21, 0x1C, 0x09, 0x4B, 0x9E, 0x46, 0x00, 0xF8, 0x20, 0x1C, 0x08, 0x4B, 0x9E, 0x46, 0x00, 0xF8, 0x35, 0x60, 0x20, 0x1C, 0x00, 0xE0, 0x03, 0x48, 0x70, 0xBC, 0x02, 0xBC, 0x08, 0x47, 0xAC, 0xB6, 0x02, 0x02
        };

        private static byte[] Function2BytecodeB = {
            0xAC, 0xA6, 0x02, 0x02, 0xA5, 0x2B, 0x00, 0x08, 0xC9, 0xA1, 0x00, 0x08
        };

        private static byte[] Function3BytecodeA = {
            10, 0xB5, 0x0C, 0x1C, 0x07, 0x49, 0x80, 0x00, 0x40, 0x18, 0x00, 0x68, 0x21, 0x1C, 0x06, 0x4B, 0x9E, 0x46, 0x00, 0xF8, 0x20, 0x1C, 0x05, 0x4B, 0x9E, 0x46, 0x00, 0xF8, 0x20, 0x1C, 0x10, 0xBC, 0x02, 0xBC, 0x08, 0x47
        };

        private static byte[] Function3BytecodeB = {
            0xA5, 0x2B, 0x00, 0x08, 0xC9, 0xA1, 0x00, 0x08, 0x90, 0x08, 0x8B, 0x08, 0x8C, 0xD4, 0x15, 0x08
        };

        private static byte[] jumpToHackBytecode = {
            0x00, 0x4B, 0x18, 0x47
        };

    }
}
