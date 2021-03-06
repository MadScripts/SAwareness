﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SAwareness
{
    class AutoLevler
    {
        private bool _usePriority;

        private int[] _priority = { 0, 0, 0, 0 };
        private int[] _sequence;

        public AutoLevler()
        {
            //LoadLevelFile();
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        ~AutoLevler()
        {
            Game.OnGameUpdate -= Game_OnGameUpdate;
        }

        public bool IsActive()
        {
            return Menu.AutoLevler.GetActive();
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            var stringList = Menu.AutoLevler.GetMenuItem("SAwarenessAutoLevlerMode").GetValue<StringList>();
            if (stringList.SelectedIndex == 1)
            {
                _usePriority = true;
                _priority = new[]
                {
                    Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerPriority")
                        .GetMenuItem("SAwarenessAutoLevlerPrioritySliderQ").GetValue<Slider>().Value,
                    Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerPriority")
                        .GetMenuItem("SAwarenessAutoLevlerPrioritySliderW").GetValue<Slider>().Value,
                    Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerPriority")
                        .GetMenuItem("SAwarenessAutoLevlerPrioritySliderE").GetValue<Slider>().Value,
                    Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerPriority")
                        .GetMenuItem("SAwarenessAutoLevlerPrioritySliderR").GetValue<Slider>().Value
                };
            }
            else
            {
                _usePriority = false;
            }

            Obj_AI_Hero player = ObjectManager.Player;
            SpellSlot[] spellSlotst = GetSortedPriotitySlots();
            if (player.SpellTrainingPoints > 0)
            {
                //TODO: Add level logic// try levelup spell, if fails level another up etc.
                if (_usePriority && Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerPriority")
                        .GetMenuItem("SAwarenessAutoLevlerPriorityActive").GetValue<bool>())
                {
                    SpellSlot[] spellSlots = GetSortedPriotitySlots();
                    for (int slotId = 0; slotId <= 3; slotId++)
                    {
                        int spellLevel = player.Spellbook.GetSpell(spellSlots[slotId]).Level;
                        player.Spellbook.LevelUpSpell(spellSlots[slotId]);
                        if (player.Spellbook.GetSpell(spellSlots[slotId]).Level != spellLevel)
                            break;
                    }
                }
                else
                {
                    if (Menu.AutoLevler.GetMenuSettings("SAwarenessAutoLevlerSequence")
                        .GetMenuItem("SAwarenessAutoLevlerSequenceActive").GetValue<bool>())
                    {
                        SpellSlot spellSlot = GetSpellSlot(_sequence[player.Level - 1]);
                        player.Spellbook.LevelUpSpell(spellSlot);

                    }
                }
            }
        }

        public void SetPriorities(int priorityQ, int priorityW, int priorityE, int priorityR)
        {
            _sequence[0] = priorityQ;
            _sequence[1] = priorityW;
            _sequence[2] = priorityE;
            _sequence[3] = priorityR;
        }

        public void SetMode(bool usePriority)
        {
            _usePriority = usePriority;
        }

        private void LoadLevelFile()
        {
            //TODO: Read Level File for sequence leveling.
            var loc = Assembly.GetExecutingAssembly().Location;
            loc = loc.Remove(loc.LastIndexOf("\\", StringComparison.Ordinal));
            loc = loc + "\\Config\\SAwareness\\autolevel.conf";
            if (!File.Exists(loc))
            {
                Download.DownloadFile("127.0.0.1", loc);
            }
            try
            {
                StreamReader sr = File.OpenText(loc);
                ReadLevelFile(sr);
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't load autolevel.conf. Using priority mode.");
                _usePriority = true;
            }
        }

        private void ReadLevelFile(StreamReader streamReader)
        {
            var sequence = new int[18];
            while (!streamReader.EndOfStream)
            {
                String line = streamReader.ReadLine();
                String champion = "";
                if (line != null && line.Length > line.IndexOf("="))
                    champion = line.Remove(line.IndexOf("="));
                if (!champion.Contains(ObjectManager.Player.ChampionName))
                    continue;
                if (line != null)
                {
                    string temp = line.Remove(0, line.IndexOf("=") + 2);
                    for (int i = 0; i < 18; i++)
                    {
                        sequence[i] = Int32.Parse(temp.Remove(1));
                        temp = temp.Remove(0, 1);
                    }
                }
                break;
            }
            _sequence = sequence;
        }

        private SpellSlot GetSpellSlot(int id)
        {
            var spellSlot = SpellSlot.Unknown;
            switch (id)
            {
                case 0:
                    spellSlot = SpellSlot.Q;
                    break;

                case 1:
                    spellSlot = SpellSlot.W;
                    break;

                case 2:
                    spellSlot = SpellSlot.E;
                    break;

                case 3:
                    spellSlot = SpellSlot.R;
                    break;
            }
            return spellSlot;
        }

        private SpellSlot[] GetSortedPriotitySlots()
        {
            var listOld = _priority;
            var listNew = new SpellSlot[4];

            listNew = ToSpellSlot(listOld, listNew);

            //listNew = listNew.OrderByDescending(c => c).ToList();



            return listNew;
        }

        private SpellSlot[] ToSpellSlot(int[] listOld, SpellSlot[] listNew)
        {
            for (int i = 0; i <= 3; i++)
            {
                switch (listOld[i])
                {
                    case 0:
                        listNew[0] = GetSpellSlot(i);
                        break;

                    case 1:
                        listNew[1] = GetSpellSlot(i);
                        break;

                    case 2:
                        listNew[2] = GetSpellSlot(i);
                        break;

                    case 3:
                        listNew[3] = GetSpellSlot(i);
                        break;
                }
            }
            return listNew;
        }

        //private List<SpellSlot> SortAlgo(List<int> listOld, List<SpellSlot> listNew)
        //{
        //    int highestPriority = -1;
        //    for (int i = 0; i < listOld.Count; i++)
        //    {
        //        int prio = _priority[i];
        //        if (highestPriority < prio)
        //        {
        //            highestPriority = prio;
        //            listNew.Add(GetSpellSlot(i));
        //            listOld.Remove(_priority[i]);
        //        }
        //    }
        //    if (listOld.Count > 1)
        //        listNew = SortAlgo(listOld, listNew);
        //    return listNew;
        //}

    }
}
