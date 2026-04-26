using System.Collections.Generic;
using Printer;
using UnityEngine;
using Utility;

public class GameManager
{
	public static GameManager Instance
	{
		get
		{
			_instance ??= new GameManager();
			return _instance;
		}
	}
    
	private static GameManager _instance;
    
	public PrintCanvas Canvas { get; private set; }
    
	public PrinterReference Reference { get; private set; }
    
	public List<PrinterPlayerController> Players { get; } = new();
    
	private bool _isPaused = false;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics()
	{
		_instance = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		_instance ??= new GameManager(); // won't recreate if already exists
	}
	
	public bool RegisterPlayer(PrinterPlayerController player)
	{
		if (Players.Contains(player))
			return false;
        
		Players.Add(player);
		return true;
	}

	public bool UnregisterPlayer(PrinterPlayerController player)
	{
		return Players.Remove(player);
	}

	public void RegisterCanvas(PrintCanvas canvas)
	{
		if (Canvas != null)
		{
			Logr.Error("Multiple PrintCanvas instances detected. This is not supported.", canvas);
			return;
		}

		Logr.Info("PrintCanvas registered.", canvas);
		Canvas = canvas;
	}

	public void UnregisterCanvas(PrintCanvas canvas)
	{
		if (canvas == null) return;
		if (canvas != Canvas)
		{
			Logr.Error("Multiple PrintCanvas instances detected. This is not supported.", canvas);
			return;
		}
            
		Logr.Info("PrintCanvas unregistered.", canvas);
		Canvas = null;
	}

	public bool RegisterReference(PrinterReference reference)
	{
		if (Reference != null)
		{
			Logr.Error("Multiple PrinterReference instances detected. This is not supported.", reference);
			return false;
		}

		Logr.Info("PrinterReference registered.", reference);
		Reference = reference;
		return true;
	}

	public bool UnregisterReference(PrinterReference reference)
	{
		if (reference == null) return false;
		if (reference != Reference)
		{
			Logr.Error("Multiple PrinterReference instances detected. This is not supported.", reference);
			return false;
		}
            
		Logr.Info("PrinterReference unregistered.", reference);
		Reference = null;
		return true;
	}

	public void SetPaused(bool paused)
	{
		if (paused == _isPaused) return;
            
		_isPaused = paused;
            
		foreach (var p in Players)
		{
			p.SetPaused(paused);
		}
	}
}