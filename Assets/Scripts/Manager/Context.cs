﻿using Gui;
using Map;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class Context
    {
        #region Private Fields

        int _lastPlayerId;

        #endregion

        #region Public Properties

        public Dictionary<int, IPlayer> Players { get; } = new Dictionary<int, IPlayer>();
        public Queue<int> PlayerOrder { get; } = new Queue<int>();
        public int CurrentPlayerId { get; set; }
        public IPlayer CurrentPlayer { get { return Players[CurrentPlayerId]; } }
        public int StartingPlayerId { get; set; }
        public GuiManager Gui { get; }
        public MapManager Map { get; }
        public AudioManager Audio { get; }
        public GameObject[] UnitPrefabs { get; }
        public Dictionary<College, Dictionary<int, IUnit>> DefaultUnits { get; }

        #endregion

        #region Consuctor

        public Context(GuiManager gui, MapManager map, AudioManager audio, GameObject[] unitPrefabs,
            Dictionary<College, Dictionary<int, IUnit>> defaultUnits)
        {
            Gui = gui;
            Map = map;
            Audio = audio;
            UnitPrefabs = unitPrefabs;
            DefaultUnits = defaultUnits;
        }

        #endregion

        #region Helper Functions

        public int GetNewPlayerId() => _lastPlayerId++;

        public void NextPlayer()
        {
            int next = PlayerOrder.Dequeue();
            CurrentPlayerId = next;
            PlayerOrder.Enqueue(next);
            if (next == StartingPlayerId)
                foreach (IPlayer player in Players.Values)
                    player.MaxMana += 5; // mana is increased each full cycle of players (this will need tweaking)
            CurrentPlayer.Mana = (int)CurrentPlayer.MaxMana;
        }

        #endregion
    }
}
