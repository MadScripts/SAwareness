﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAwareness
{
    class Recall
    {

        public List<RecallInfo> _recalls = new List<RecallInfo>();

        public class RecallInfo
        {
            public int NetworkId;
            public Packet.S2C.Recall.Struct Recall;
            public Packet.S2C.Recall.Struct Recall2;
            public int StartTime;

            public RecallInfo(int networkId)
            {
                NetworkId = networkId;
            }
        }

        public Recall()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsEnemy)
                {
                    _recalls.Add(new RecallInfo(enemy.NetworkId));
                }
            }
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
        }

        ~Recall()
        {
            Game.OnGameProcessPacket -= Game_OnGameProcessPacket;
        }

        public bool IsActive()
        {
            return Menu.RecallDetector.GetActive();
        }

        void Game_OnGameProcessPacket(GamePacketEventArgs args) //TODO: Check for Packet id
        {
            if (!IsActive())
                return;
            try
            {
                var reader = new BinaryReader(new MemoryStream(args.PacketData));
                byte PacketId = reader.ReadByte(); //PacketId
                if (PacketId != Packet.S2C.Recall.Header) //OLD 215
                    return;
                Packet.S2C.Recall.Struct recall = Packet.S2C.Recall.Decoded(args.PacketData);
                HandleRecall(recall);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RecallProcess: " + ex.ToString());
                return;
            }

        }

        void HandleRecall(Packet.S2C.Recall.Struct recallEx)
        {
            int time = Environment.TickCount - Game.Ping;

            foreach (RecallInfo recall in _recalls)
            {
                if (recall == null) continue;

                if (recallEx.Type == Packet.S2C.Recall.ObjectType.Player)
                {
                    var obj = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recall.NetworkId);
                    var objEx = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recallEx.UnitNetworkId);
                    if (obj == null)
                        continue;
                    if (obj.NetworkId == objEx.NetworkId) //already existing
                    {
                        recall.Recall = recallEx;
                        recall.Recall2 = new Packet.S2C.Recall.Struct();
                        StringList t = Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorMode").GetValue<StringList>();
                        if (t.SelectedIndex == 0 || t.SelectedIndex == 2)
                        {
                            if (recallEx.Status == Packet.S2C.Recall.RecallStatus.TeleportStart || recallEx.Status == Packet.S2C.Recall.RecallStatus.RecallStarted)
                            {
                                recall.StartTime = (int)Game.Time;
                                if (Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorLocalChat").GetValue<bool>())
                                {
                                    Game.PrintChat(obj.ChampionName + " porting with {0} hp", (int)obj.Health);
                                }
                                else
                                {
                                    Game.Say(obj.ChampionName + " porting with {0} hp", (int)obj.Health);
                                }
                            }
                            else if (recallEx.Status == Packet.S2C.Recall.RecallStatus.TeleportEnd || recallEx.Status == Packet.S2C.Recall.RecallStatus.RecallFinished)
                            {
                                //recall.StartTime = 0;
                                if (Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorLocalChat").GetValue<bool>())
                                {
                                    Game.PrintChat(obj.ChampionName + " ported with {0} hp", (int)obj.Health);
                                }
                                else
                                {
                                    Game.Say(obj.ChampionName + " ported with {0} hp", (int)obj.Health);
                                }
                            }
                            else
                            {
                                //recall.StartTime = 0;
                                if (Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorLocalChat").GetValue<bool>())
                                {
                                    Game.PrintChat(obj.ChampionName + " canceled with {0} hp", (int)obj.Health);
                                }
                                else
                                {
                                    Game.Say(obj.ChampionName + " canceled with {0} hp", (int)obj.Health);
                                }
                            }
                        }
                        return;
                    }
                }
                else if (recallEx.Status == Packet.S2C.Recall.RecallStatus.TeleportStart || recallEx.Status == Packet.S2C.Recall.RecallStatus.TeleportEnd)
                {
                    if (recall.Recall.Status == Packet.S2C.Recall.RecallStatus.TeleportStart)
                        recall.Recall2 = recallEx;

                    var obj = ObjectManager.GetUnitByNetworkId<GameObject>(recallEx.UnitNetworkId);
                    var screen = obj.Position;
                    for (int i = 0; i < Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorPingTimes").GetValue<Slider>().Value; i++)
                    {
                        GamePacket gPacketT = Packet.C2S.Ping.Encoded(new Packet.C2S.Ping.Struct(screen.X, screen.Y, 0, Packet.PingType.Danger));
                        if (Menu.RecallDetector.GetMenuItem("SAwarenessRecallDetectorLocalPing").GetValue<bool>())
                        {
                            //TODO: Add local ping
                        }
                        else
                        {
                            gPacketT.Send();
                        }
                    }
                }
            }
        }
    }
}
