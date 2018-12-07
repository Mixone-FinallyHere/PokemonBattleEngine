﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Packets;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kermalis.PokemonBattleEngine.Data
{
    public sealed class PBEPokemon
    {
        public readonly byte Id;
        // Not included in ToBytes() or FromBytes(). Set manually by the host
        // True indicates this Pokémon is owned by the client or team 0 in the eyes of the host/spectators
        public bool LocalTeam;
        public readonly PBEPokemonShell Shell;

        public string NameForTrainer(bool firstLetterCapitalized)
        {
            string prefix;
            if (firstLetterCapitalized)
            {
                if (LocalTeam)
                {
                    prefix = "";
                }
                else
                {
                    prefix = "The foe's ";
                }
            }
            else
            {
                if (LocalTeam)
                {
                    prefix = "";
                }
                else
                {
                    prefix = "the foe's ";
                }
            }
            return prefix + Shell.Nickname;
        }
        public string NameWithGender => Shell.Nickname + GenderSymbol;
        public string GenderSymbol => Shell.Gender == PBEGender.Female ? "♀" : Shell.Gender == PBEGender.Male ? "♂" : string.Empty;

        public ushort HP, MaxHP, Attack, Defense, SpAttack, SpDefense, Speed;
        public PBEMove[] Moves = new PBEMove[PBESettings.NumMoves];
        public byte[] PP = new byte[PBESettings.NumMoves], MaxPP = new byte[PBESettings.NumMoves];

        public PBESpecies Species;
        public bool Shiny;
        public PBEAbility Ability;
        public PBEType Type1, Type2;
        public PBEItem Item;
        public PBEFieldPosition FieldPosition;
        public PBEStatus1 Status1;
        public PBEStatus2 Status2;
        // These are in a set order; see BattleEffects->ApplyStatChange()
        public sbyte AttackChange, DefenseChange, SpAttackChange, SpDefenseChange, SpeedChange, AccuracyChange, EvasionChange;

        public byte Status1Counter; // Toxic/Sleep
        public byte SleepTurns; // Amount of turns to Sleep

        public byte ConfusionCounter; // Confused
        public byte ConfusionTurns; // Amount of turns to be Confused

        public byte ProtectCounter; // Protect
        public ushort SubstituteHP; // Substitute

        public PBEFieldPosition SeededPosition; // The position to return Leech Seed HP to on the opposing team

        public PBEAction PreviousAction, LockedAction, SelectedAction;

        // Stats & PP are set from the shell info, but LocalTeam will need to be manually set by the host
        public PBEPokemon(byte id, PBEPokemonShell shell)
        {
            Shell = shell;
            Species = Shell.Species;
            Shiny = Shell.Shiny;
            Ability = Shell.Ability;
            Item = Shell.Item;
            Id = id;
            SelectedAction.PokemonId = id;
            CalculateStats();
            HP = MaxHP;
            Moves = Shell.Moves;
            for (int i = 0; i < PBESettings.NumMoves; i++)
            {
                PBEMove move = Shell.Moves[i];
                if (move != PBEMove.None)
                {
                    byte tier = PBEMoveData.Data[move].PPTier;
                    int movePP = (tier * PBESettings.PPMultiplier) + (tier * Shell.PPUps[i]);
                    PP[i] = MaxPP[i] = (byte)movePP;
                }
            }
            Type1 = PBEPokemonData.Data[Species].Type1;
            Type2 = PBEPokemonData.Data[Species].Type2;
        }
        // This constructor is to define an unknown remote Pokémon
        // LocalTeam is set to false here
        public PBEPokemon(PBEPkmnSwitchInPacket psip)
        {
            Id = psip.PokemonId;
            LocalTeam = false;
            Shell = new PBEPokemonShell
            {
                Species = psip.Species,
                Shiny = psip.Shiny,
                Nickname = psip.Nickname,
                Level = psip.Level,
                Gender = psip.Gender,
                Ability = PBEAbility.MAX,
                Item = PBEItem.MAX,
                Nature = PBENature.MAX
            };
            Species = psip.Species;
            Shiny = psip.Shiny;
            Ability = PBEAbility.MAX;
            Item = PBEItem.MAX;
            for (int i = 0; i < PBESettings.NumMoves; i++)
            {
                Moves[i] = PBEMove.MAX;
            }

            Type1 = PBEPokemonData.Data[Species].Type1;
            Type2 = PBEPokemonData.Data[Species].Type2;
        }

        public bool HasType(PBEType type) => Type1 == type || Type2 == type;

        void CalculateStats()
        {
            PBEPokemonData pData = PBEPokemonData.Data[Species];

            MaxHP = (ushort)(((2 * pData.HP + Shell.IVs[0] + (Shell.EVs[0] / 4)) * Shell.Level / PBESettings.MaxLevel) + Shell.Level + 10);

            int i = 0;
            ushort OtherStat(byte baseVal)
            {
                double natureMultiplier = 1 + (PBEPokemonData.NatureBoosts[Shell.Nature][i] * PBESettings.NatureStatBoost);
                ushort val = (ushort)((((2 * baseVal + Shell.IVs[i + 1] + (Shell.EVs[i + 1] / 4)) * Shell.Level / PBESettings.MaxLevel) + 5) * natureMultiplier);
                i++;
                return val;
            }
            Attack = OtherStat(pData.Attack);
            Defense = OtherStat(pData.Defense);
            SpAttack = OtherStat(pData.SpAttack);
            SpDefense = OtherStat(pData.SpDefense);
            Speed = OtherStat(pData.Speed);
        }

        public void ClearForSwitch()
        {
            FieldPosition = PBEFieldPosition.None;
            Species = Shell.Species;
            Ability = Shell.Ability;
            Shiny = Shell.Shiny;

            AttackChange = DefenseChange = SpAttackChange = SpDefenseChange = SpeedChange = AccuracyChange = EvasionChange = 0;

            if (Status1 == PBEStatus1.Asleep)
            {
                Status1Counter = SleepTurns;
            }
            else if (Status1 == PBEStatus1.BadlyPoisoned)
            {
                Status1Counter = 1;
            }

            Status2 &= ~PBEStatus2.Confused;
            ConfusionCounter = ConfusionTurns = 0;
            Status2 &= ~PBEStatus2.LeechSeed;
            SeededPosition = PBEFieldPosition.None;
            Status2 &= ~PBEStatus2.Pumped;
            Status2 &= ~PBEStatus2.Substitute;
            SubstituteHP = 0;
            Status2 &= ~PBEStatus2.Transformed;

            if (Shell.Nature != PBENature.MAX) // If the nature is unset, the program is not the host and does not own the Pokémon
            {
                CalculateStats();
            }
        }

        // Transforms into "target" and sets both Pokémons' information to the parameters
        // Also sets the Status2 transformed bit
        public void Transform(PBEPokemon target, ushort targetAttack, ushort targetDefense, ushort targetSpAttack, ushort targetSpDefense, ushort targetSpeed, PBEAbility targetAbility, PBEType targetType1, PBEType targetType2, PBEMove[] targetMoves)
        {
            Species = target.Species;
            Shiny = target.Shiny;
            Ability = target.Ability = targetAbility;
            Type1 = target.Type1 = targetType1;
            Type2 = target.Type2 = targetType2;
            Attack = target.Attack = targetAttack;
            Defense = target.Defense = targetDefense;
            SpAttack = target.SpAttack = targetSpAttack;
            SpDefense = target.SpDefense = targetSpDefense;
            Speed = target.Speed = targetSpeed;
            AttackChange = target.AttackChange;
            DefenseChange = target.DefenseChange;
            SpAttackChange = target.SpAttackChange;
            SpDefenseChange = target.SpDefenseChange;
            SpeedChange = target.SpeedChange;
            AccuracyChange = target.AccuracyChange;
            EvasionChange = target.EvasionChange;
            Moves = target.Moves = targetMoves;
            for (int i = 0; i < PBESettings.NumMoves; i++)
            {
                PP[i] = MaxPP[i] = PBESettings.PPMultiplier;
            }

            Status2 |= PBEStatus2.Transformed;
        }

        public PBEType GetHiddenPowerType()
        {
            int a = Shell.IVs[0] & 1,
                b = Shell.IVs[1] & 1,
                c = Shell.IVs[2] & 1,
                d = Shell.IVs[5] & 1,
                e = Shell.IVs[3] & 1,
                f = Shell.IVs[4] & 1;
            return PBEPokemonData.HiddenPowerTypes[((1 << 0) * a + (1 << 1) * b + (1 << 2) * c + (1 << 3) * d + (1 << 4) * e + (1 << 5) * f) * (PBEPokemonData.HiddenPowerTypes.Length - 1) / ((1 << 6) - 1)];
        }
        public int GetHiddenPowerBasePower()
        {
            int a = (Shell.IVs[0] & 2) == 2 ? 1 : 0,
                b = (Shell.IVs[1] & 2) == 2 ? 1 : 0,
                c = (Shell.IVs[2] & 2) == 2 ? 1 : 0,
                d = (Shell.IVs[5] & 2) == 2 ? 1 : 0,
                e = (Shell.IVs[3] & 2) == 2 ? 1 : 0,
                f = (Shell.IVs[4] & 2) == 2 ? 1 : 0;
            // 30 is minimum, 30+40 is maximum
            return (((1 << 0) * a + (1 << 1) * b + (1 << 2) * c + (1 << 3) * d + (1 << 4) * e + (1 << 5) * f) * 40 / ((1 << 6) - 1)) + 30;
        }

        // ToBytes() and FromBytes() will only be used when the server sends you your team Ids, so they do not need to contain all info
        internal byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.Add(Id);
            bytes.AddRange(Shell.ToBytes());
            return bytes.ToArray();
        }
        internal static PBEPokemon FromBytes(BinaryReader r)
        {
            return new PBEPokemon(r.ReadByte(), PBEPokemonShell.FromBytes(r));
        }

        public override bool Equals(object obj)
        {
            if (obj is PBEPokemon other)
            {
                return other.Id.Equals(Id);
            }

            return base.Equals(obj);
        }
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString()
        {
            bool remotePokemon = Shell.Nature == PBENature.MAX; // If the nature is unset, the program is not the host and does not own the Pokémon

            string item = Item.ToString().Replace("MAX", "???");
            string nature = Shell.Nature.ToString().Replace("MAX", "???");
            string ability = Ability.ToString().Replace("MAX", "???");
            string[] moveStrs = new string[PBESettings.NumMoves];
            for (int i = 0; i < PBESettings.NumMoves; i++)
            {
                string mStr = Moves[i].ToString().Replace("MAX", "???");
                if (!remotePokemon)
                {
                    mStr += $" {PP[i]}/{MaxPP[i]}";
                }

                moveStrs[i] = mStr;
            }
            string moves = moveStrs.Print(false);

            var sb = new StringBuilder();

            sb.AppendLine($"{Shell.Nickname}/{Species} {GenderSymbol} Lv.{Shell.Level}");
            sb.AppendLine($"HP: {HP}/{MaxHP} ({(double)HP / MaxHP:P2})");
            sb.AppendLine($"Status1: {Status1}");
            sb.AppendLine($"Status2: {Status2}");
            if (!remotePokemon && Status2.HasFlag(PBEStatus2.Substitute))
            {
                sb.AppendLine($"Substitute HP: {SubstituteHP}");
            }

            sb.AppendLine($"Item: {item}");
            sb.AppendLine($"Ability: {ability}");
            if (!remotePokemon)
            {
                sb.AppendLine($"Nature: {nature}");
            }

            if (!remotePokemon)
            {
                sb.AppendLine($"Hidden Power: {GetHiddenPowerType()}/{GetHiddenPowerBasePower()}");
            }

            sb.Append($"Moves: {moves}");

            return sb.ToString();
        }
    }
}
