using System;
using System.Runtime.Serialization;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Framework;
using WaveEngine.Networking;
using WaveEngine.Networking.Messages;

namespace Codigo
{
    /**
     * <summary>
     * This class holds and syncs data that will not change often
     * </summary>
     */
    [DataContract]
    public class PlayerDataSync : NetworkSyncComponent
    {
        [RequiredComponent]
        private Player player;
        [DataMember]
        private string lastName=string.Empty;
        [DataMember]
        private Color lastColor=Color.Black;
        [DataMember]
        private int lastTeam = -1;

        //It should be synced for clients selecting WOs as well
        public override bool NeedSendSyncData()
        {
            return player.playerColor != lastColor || player.playerName != lastName || player.playerTeam != lastTeam;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            player.playerName=binaryReader.ReadString();
            Vector4 vect = new Vector4(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
            player.playerColor = Color.FromVector4(ref vect);
            player.playerTeam = binaryReader.ReadInt32();
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastName = player.playerName;
            lastColor = player.playerColor;
            lastTeam = player.playerTeam;
            Vector4 vect = lastColor.ToVector4();
            writer.Write(lastName);
            writer.Write(vect.X);
            writer.Write(vect.Y);
            writer.Write(vect.Z);
            writer.Write(vect.W);
            writer.Write(lastTeam);

        }
    }
}