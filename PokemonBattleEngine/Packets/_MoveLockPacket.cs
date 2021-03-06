﻿using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using System.Collections.ObjectModel;
using System.IO;

namespace Kermalis.PokemonBattleEngine.Packets
{
    public sealed class PBEMoveLockPacket : IPBEPacket
    {
        public const ushort Code = 0x28;
        public ReadOnlyCollection<byte> Data { get; }

        public PBETrainer MoveUserTrainer { get; }
        public PBEFieldPosition MoveUser { get; }
        public PBEMoveLockType MoveLockType { get; }
        public PBEMove LockedMove { get; }
        public PBETurnTarget? LockedTargets { get; }

        internal PBEMoveLockPacket(PBEBattlePokemon moveUser, PBEMoveLockType moveLockType, PBEMove lockedMove, PBETurnTarget? lockedTargets = null)
        {
            using (var ms = new MemoryStream())
            using (var w = new EndianBinaryWriter(ms, encoding: EncodingType.UTF16))
            {
                w.Write(Code);
                w.Write((MoveUserTrainer = moveUser.Trainer).Id);
                w.Write(MoveUser = moveUser.FieldPosition);
                w.Write(MoveLockType = moveLockType);
                w.Write(LockedMove = lockedMove);
                w.Write(lockedTargets.HasValue);
                if (lockedTargets.HasValue)
                {
                    w.Write((LockedTargets = lockedTargets).Value);
                }
                Data = new ReadOnlyCollection<byte>(ms.GetBuffer());
            }
        }
        internal PBEMoveLockPacket(byte[] data, EndianBinaryReader r, PBEBattle battle)
        {
            Data = new ReadOnlyCollection<byte>(data);
            MoveUserTrainer = battle.Trainers[r.ReadByte()];
            MoveUser = r.ReadEnum<PBEFieldPosition>();
            MoveLockType = r.ReadEnum<PBEMoveLockType>();
            LockedMove = r.ReadEnum<PBEMove>();
            if (r.ReadBoolean())
            {
                LockedTargets = r.ReadEnum<PBETurnTarget>();
            }
        }
    }
}
