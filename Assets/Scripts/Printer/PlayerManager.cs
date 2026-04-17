using System.Collections.Generic;
using UnityEngine;

namespace Printer
{
    public class PlayerManager
    {
        private static bool _isPaused = false;
        
        public static List<PrinterPlayerController> Players { get; } = new();
        
        public static void RegisterPlayer(PrinterPlayerController player)
        {
            if (!Players.Contains(player))
                Players.Add(player);
        }

        public static void UnregisterPlayer(PrinterPlayerController player)
        {
            Players.Remove(player);
        }

        public static void SetPaused(bool paused)
        {
            if (paused == _isPaused) return;
            
            _isPaused = paused;
            
#if UNITY_EDITOR
            Debug.Log($"{nameof(PlayerManager)} is paused: {_isPaused}");
#endif

            foreach (var p in Players)
            {
                p.SetPaused(paused);
            }
        }
    }
}