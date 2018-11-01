﻿using Ether.Network.Packets;
using System.Collections.Generic;

namespace Kermalis.PokemonBattleEngine.Packets
{
    public sealed class PRequestActionsPacket : INetPacket
    {
        public const short Code = 0x07;
        public IEnumerable<byte> Buffer => new byte[] { 0x02, 0x00, 0x07, 0x00 };

        public PRequestActionsPacket() { }
        public PRequestActionsPacket(byte[] buffer) { }

        public void Dispose() { }
    }
}