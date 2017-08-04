using Codigo.GUI;
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
     * This class holds and syncs data to customize your player before the game
     * </summary>
     */
    [DataContract]
    public class PlayerInfoSync : NetworkSyncComponent
    {
        [RequiredComponent]
        private PlayerInfo player;
        [DataMember]
        private int lastTeam=-1;
        [DataMember]
        private int lastColor=-1;

        [DataMember]
        private bool lastReady;

        public override bool NeedSendSyncData()
        {
            return player.playerColor != lastColor || player.playerTeam != lastTeam ||player.ready!=lastReady;
        }

        public override void ReadSyncData(IncomingMessage binaryReader)
        {
            player.playerColor = binaryReader.ReadInt32();
            player.playerTeam=binaryReader.ReadInt32();

            player.nextColor.Text = ClientDispatcher.colorNames[player.playerColor];
            player.textTeam.Text = player.playerTeam == -1 ? "No team" : "Team " + player.playerTeam;
            player.nextColor.Foreground = ClientDispatcher.colors[player.playerColor];
            player.nextColor.BorderColor = ClientDispatcher.colors[player.playerColor];

            player.ready = binaryReader.ReadBoolean();
            player.ChangeReady();
        }

        public override void WriteSyncData(OutgoingMessage writer)
        {
            lastTeam = player.playerTeam;
            lastColor = player.playerColor;
            lastReady = player.ready;

            writer.Write(lastColor);
            writer.Write(lastTeam);
            writer.Write(lastReady);

        }
    }
}