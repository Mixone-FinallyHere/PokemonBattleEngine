﻿using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kermalis.PokemonBattleEngineTesting
{
    class LocalizationDumper
    {
        // You must dump the NARC files yourself (/a/0/0/2 in each language)
        public static void Dump()
        {
            using (var english = new NARC(@"../../../\DumpedData\W2EnglishTexts.narc"))
            using (var french = new NARC(@"../../../\DumpedData\W2FrenchTexts.narc"))
            using (var german = new NARC(@"../../../\DumpedData\W2GermanTexts.narc"))
            using (var italian = new NARC(@"../../../\DumpedData\W2ItalianTexts.narc"))
            using (var japanese = new NARC(@"../../../\DumpedData\W2JapaneseTexts.narc"))
            using (var korean = new NARC(@"../../../\DumpedData\W2KoreanTexts.narc"))
            using (var spanish = new NARC(@"../../../\DumpedData\W2SpanishTexts.narc"))
            {
                string[][] eng, fre, ger, ita, jap, kor, spa;
                void LoadTexts(int fileNum)
                {
                    string[][] ReadTextFile(NARC narc)
                    {
                        using (var r = new EndianBinaryReader(narc.Files[fileNum], Endianness.LittleEndian))
                        {
                            ushort numBlocks = r.ReadUInt16();
                            ushort numEntries = r.ReadUInt16();
                            r.ReadUInt32(); // fileSize
                            r.ReadUInt32(); // padding
                            var texts = new string[numBlocks][];
                            var blockOffsets = new uint[numBlocks];
                            for (int i = 0; i < numBlocks; i++)
                            {
                                texts[i] = new string[numEntries];
                                blockOffsets[i] = r.ReadUInt32();
                            }
                            for (int i = 0; i < numBlocks; i++)
                            {
                                r.BaseStream.Position = blockOffsets[i];
                                r.ReadUInt32(); // blockSize
                                var stringOffsets = new uint[numEntries];
                                var stringLengths = new ushort[numEntries];
                                for (int j = 0; j < numEntries; j++)
                                {
                                    stringOffsets[j] = r.ReadUInt32();
                                    stringLengths[j] = r.ReadUInt16();
                                    r.ReadUInt16(); // textFlags[j]
                                }
                                for (int j = 0; j < numEntries; j++)
                                {
                                    r.BaseStream.Position = blockOffsets[i] + stringOffsets[j];
                                    var encoded = new ushort[stringLengths[j]];
                                    for (int k = 0; k < stringLengths[j]; k++)
                                    {
                                        encoded[k] = r.ReadUInt16();
                                    }
                                    int key = encoded[stringLengths[j] - 1] ^ 0xFFFF;
                                    var decoded = new int[stringLengths[j]];
                                    for (int k = stringLengths[j] - 1; k >= 0; k--)
                                    {
                                        decoded[k] = encoded[k] ^ key;
                                        key = ((key >> 3) | (key << 13)) & 0xFFFF;
                                    }
                                    for (int k = 0; k < stringLengths[j]; k++)
                                    {
                                        int c = decoded[k];
                                        if (c == 0xFFFF)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            string car;
                                            switch (c)
                                            {
                                                case '"': car = "\\\""; break;
                                                case 0x246D: car = "♂"; break;
                                                case 0x246E: car = "♀"; break;
                                                case 0x2486: car = "[PK]"; break;
                                                case 0x2487: car = "[MN]"; break;
                                                case 0xFFFE: car = "\\n"; break;
                                                default: car = ((char)c).ToString(); break;
                                            }
                                            texts[i][j] += car;
                                        }
                                    }
                                }
                            }
                            return texts;
                        }
                    }

                    eng = ReadTextFile(english);
                    fre = ReadTextFile(french);
                    ger = ReadTextFile(german);
                    ita = ReadTextFile(italian);
                    jap = ReadTextFile(japanese);
                    kor = ReadTextFile(korean);
                    spa = ReadTextFile(spanish);
                }

                using (var writer = new StreamWriter(@"../../../../\PokemonBattleEngine\Localization\AbilityLocalization.cs"))
                {
                    IEnumerable<PBEAbility> allAbilities = new[] { PBEAbility.None }.Concat(Enum.GetValues(typeof(PBEAbility)).Cast<PBEAbility>().Except(new[] { PBEAbility.None, PBEAbility.MAX }).OrderBy(e => e.ToString()));
                    PBEAbility lastAbility = allAbilities.Last();
                    void WriteAll()
                    {
                        writer.WriteLine("        {");
                        foreach (PBEAbility ability in allAbilities)
                        {
                            byte i = (byte)ability;
                            writer.WriteLine($"            {{ PBEAbility.{ability}, new PBELocalizedString(\"{eng[0][i]}\", \"{fre[0][i]}\", \"{ger[0][i]}\", \"{ita[0][i]}\", \"{jap[0][i]}\", \"{jap[1][i]}\", \"{kor[0][i]}\", \"{spa[0][i]}\") }}{(ability == lastAbility ? string.Empty : ",")}");
                        }
                        writer.WriteLine("        });");
                    }
                    writer.WriteLine("using Kermalis.PokemonBattleEngine.Data;");
                    writer.WriteLine("using System.Collections.Generic;");
                    writer.WriteLine("using System.Collections.ObjectModel;");
                    writer.WriteLine();
                    writer.WriteLine("namespace Kermalis.PokemonBattleEngine.Localization");
                    writer.WriteLine("{");
                    writer.WriteLine("    public static class PBEAbilityLocalization");
                    writer.WriteLine("    {");
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEAbility, PBELocalizedString> Names { get; } = new ReadOnlyDictionary<PBEAbility, PBELocalizedString>(new Dictionary<PBEAbility, PBELocalizedString>()");
                    LoadTexts(374);
                    WriteAll();
                    writer.WriteLine();
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEAbility, PBELocalizedString> Descriptions { get; } = new ReadOnlyDictionary<PBEAbility, PBELocalizedString>(new Dictionary<PBEAbility, PBELocalizedString>()");
                    LoadTexts(375);
                    WriteAll();
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
                using (var writer = new StreamWriter(@"../../../../\PokemonBattleEngine\Localization\ItemLocalization.cs"))
                {
                    IEnumerable<PBEItem> allItems = new[] { PBEItem.None }.Concat(Enum.GetValues(typeof(PBEItem)).Cast<PBEItem>().Except(new[] { PBEItem.None }).OrderBy(e => e.ToString()));
                    PBEItem lastItem = allItems.Last();
                    void WriteAll()
                    {
                        writer.WriteLine("        {");
                        foreach (PBEItem item in allItems)
                        {
                            ushort i = (ushort)item;
                            writer.WriteLine($"            {{ PBEItem.{item}, new PBELocalizedString(\"{eng[0][i]}\", \"{fre[0][i]}\", \"{ger[0][i]}\", \"{ita[0][i]}\", \"{jap[0][i]}\", \"{jap[1][i]}\", \"{kor[0][i]}\", \"{spa[0][i]}\") }}{(item == lastItem ? string.Empty : ",")}");
                        }
                        writer.WriteLine("        });");
                    }
                    writer.WriteLine("using Kermalis.PokemonBattleEngine.Data;");
                    writer.WriteLine("using System.Collections.Generic;");
                    writer.WriteLine("using System.Collections.ObjectModel;");
                    writer.WriteLine();
                    writer.WriteLine("namespace Kermalis.PokemonBattleEngine.Localization");
                    writer.WriteLine("{");
                    writer.WriteLine("    public static class PBEItemLocalization");
                    writer.WriteLine("    {");
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEItem, PBELocalizedString> Names { get; } = new ReadOnlyDictionary<PBEItem, PBELocalizedString>(new Dictionary<PBEItem, PBELocalizedString>()");
                    LoadTexts(64);
                    WriteAll();
                    writer.WriteLine();
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEItem, PBELocalizedString> Descriptions { get; } = new ReadOnlyDictionary<PBEItem, PBELocalizedString>(new Dictionary<PBEItem, PBELocalizedString>()");
                    LoadTexts(63);
                    WriteAll();
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
                using (var writer = new StreamWriter(@"../../../../\PokemonBattleEngine\Localization\MoveLocalization.cs"))
                {
                    const ushort lastMove = (ushort)(PBEMove.MAX - 1);
                    void WriteAll()
                    {
                        writer.WriteLine("        {");
                        for (ushort i = 0; i <= lastMove; i++)
                        {
                            writer.WriteLine($"            {(Enum.IsDefined(typeof(PBEMove), i) ? string.Empty : "// ")}{{ PBEMove.{(PBEMove)i}, new PBELocalizedString(\"{eng[0][i]}\", \"{fre[0][i]}\", \"{ger[0][i]}\", \"{ita[0][i]}\", \"{jap[0][i]}\", \"{jap[1][i]}\", \"{kor[0][i]}\", \"{spa[0][i]}\") }}{(i == lastMove ? string.Empty : ",")}");
                        }
                        writer.WriteLine("        });");
                    }
                    writer.WriteLine("using Kermalis.PokemonBattleEngine.Data;");
                    writer.WriteLine("using System.Collections.Generic;");
                    writer.WriteLine("using System.Collections.ObjectModel;");
                    writer.WriteLine();
                    writer.WriteLine("namespace Kermalis.PokemonBattleEngine.Localization");
                    writer.WriteLine("{");
                    writer.WriteLine("    public static class PBEMoveLocalization");
                    writer.WriteLine("    {");
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEMove, PBELocalizedString> Names { get; } = new ReadOnlyDictionary<PBEMove, PBELocalizedString>(new Dictionary<PBEMove, PBELocalizedString>()");
                    LoadTexts(403);
                    WriteAll();
                    writer.WriteLine();
                    writer.WriteLine("        public static ReadOnlyDictionary<PBEMove, PBELocalizedString> Descriptions { get; } = new ReadOnlyDictionary<PBEMove, PBELocalizedString>(new Dictionary<PBEMove, PBELocalizedString>()");
                    LoadTexts(402);
                    WriteAll();
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
                using (var writer = new StreamWriter(@"../../../../\PokemonBattleEngine\Localization\PokemonLocalization.cs"))
                {
                    IEnumerable<PBESpecies> allSpecies = Enum.GetValues(typeof(PBESpecies)).Cast<PBESpecies>().Where(e => (uint)e >> 0x10 == 0).OrderBy(e => e.ToString());
                    PBESpecies lastSpecies = allSpecies.Last();
                    void WriteAll()
                    {
                        writer.WriteLine("        {");
                        foreach (PBESpecies species in allSpecies)
                        {
                            uint i = (uint)species;
                            writer.WriteLine($"            {{ PBESpecies.{(PBESpecies)i}, new PBELocalizedString(\"{eng[0][i]}\", \"{fre[0][i]}\", \"{ger[0][i]}\", \"{ita[0][i]}\", \"{jap[0][i]}\", \"{jap[1][i]}\", \"{kor[0][i]}\", \"{spa[0][i]}\") }}{(species == lastSpecies ? string.Empty : ",")}");
                        }
                        writer.WriteLine("        });");
                    }
                    writer.WriteLine("using Kermalis.PokemonBattleEngine.Data;");
                    writer.WriteLine("using System.Collections.Generic;");
                    writer.WriteLine("using System.Collections.ObjectModel;");
                    writer.WriteLine();
                    writer.WriteLine("namespace Kermalis.PokemonBattleEngine.Localization");
                    writer.WriteLine("{");
                    writer.WriteLine("    public static class PBEPokemonLocalization");
                    writer.WriteLine("    {");
                    writer.WriteLine("        public static ReadOnlyDictionary<PBESpecies, PBELocalizedString> Names { get; } = new ReadOnlyDictionary<PBESpecies, PBELocalizedString>(new Dictionary<PBESpecies, PBELocalizedString>()");
                    LoadTexts(90);
                    WriteAll();
                    writer.WriteLine();
                    writer.WriteLine("        public static ReadOnlyDictionary<PBESpecies, PBELocalizedString> Entries { get; } = new ReadOnlyDictionary<PBESpecies, PBELocalizedString>(new Dictionary<PBESpecies, PBELocalizedString>()");
                    LoadTexts(442);
                    WriteAll();
                    writer.WriteLine();
                    writer.WriteLine("        public static ReadOnlyDictionary<PBESpecies, PBELocalizedString> Categories { get; } = new ReadOnlyDictionary<PBESpecies, PBELocalizedString>(new Dictionary<PBESpecies, PBELocalizedString>()");
                    LoadTexts(464);
                    WriteAll();
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
            }
        }
    }
}
