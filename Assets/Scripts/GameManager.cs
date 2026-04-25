using System.Collections.Generic;
using Printer;
using UnityEngine;

public static class GameManager
{
    public static PrintCanvas Canvas { get; private set; }
    
    public static PrinterReference Reference { get; private set; }
    
    public static List<PrinterPlayerController> Players { get; } = new();
    
    private static bool _isPaused = false;

    static GameManager()
    {
        GameMaster.Scripts.GameMasterUI.OnToggle += SetPaused;
    }
        
    public static void RegisterPlayer(PrinterPlayerController player)
    {
        if (!Players.Contains(player))
            Players.Add(player);
    }

    public static void UnregisterPlayer(PrinterPlayerController player)
    {
        Players.Remove(player);
    }

    public static void RegisterCanvas(PrintCanvas canvas)
    {
        if (Canvas != null)
        {
            Debug.LogError("Multiple PrintCanvas instances detected. This is not supported.", canvas);
            return;
        }

        Canvas = canvas;
    }

    public static void UnregisterCanvas(PrintCanvas canvas)
    {
        if (canvas == null) return;
        if (canvas != Canvas)
        {
            Debug.LogError("Multiple PrintCanvas instances detected. This is not supported.", Canvas);
            return;
        }
            
        Canvas = null;
    }

    public static void RegisterReference(PrinterReference reference)
    {
        if (Canvas != null)
        {
            Debug.LogError("Multiple PrintCanvas instances detected. This is not supported.", reference);
            return;
        }

        Reference = reference;
    }

    public static void UnregisterReference(PrinterReference reference)
    {
        if (reference == null) return;
        if (reference != Reference)
        {
            Debug.LogError("Multiple Printreference instances detected. This is not supported.", reference);
            return;
        }
            
        Reference = null;
    }

    public static void SetPaused(bool paused)
    {
        if (paused == _isPaused) return;
            
        _isPaused = paused;
            
        foreach (var p in Players)
        {
            p.SetPaused(paused);
        }
    }
}