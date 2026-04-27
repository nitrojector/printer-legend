using System.Collections.Generic;
using Printer;
using UnityEngine;
using Utility;
using WindowContents;

public class GameMgr
{
	public static GameMgr Instance
	{
		get
		{
			_instance ??= new GameMgr();
			return _instance;
		}
	}
    
	private static GameMgr _instance;
    
	
	public PrinterViewWindowContent PrinterViewWC { get; private set; }
    
	public bool IsPrinterViewRegistered => PrinterViewWC != null;
	
	public PrinterReferenceWindowContent PrinterReferenceWC { get; private set; }
	
	public bool IsPrinterReferenceRegistered => PrinterReferenceWC != null;
    
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
		_instance ??= new GameMgr(); // won't recreate if already exists
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
	

	public void RegisterPrinterView(PrinterViewWindowContent canvas)
	{
		if (PrinterViewWC != null)
		{
			Logr.Error("Multiple PrinterViewWindowContent instances detected. This is not supported.", canvas);
			return;
		}

		Logr.Info("PrinterViewWindowContent registered.", canvas);
		PrinterViewWC = canvas;
	}

	public void UnregisterPrinterView(PrinterViewWindowContent canvas)
	{
		if (canvas == null) return;
		if (canvas != PrinterViewWC)
		{
			Logr.Error("Multiple PrinterViewWindowContent instances detected. This is not supported.", canvas);
			return;
		}
            
		Logr.Info("PrinterViewWindowContent unregistered.", canvas);
		PrinterViewWC = null;
	}

	public bool RegisterPrinterReference(PrinterReferenceWindowContent reference)
	{
		if (PrinterReferenceWC != null)
		{
			Logr.Error("Multiple PrinterReferenceWindowContent instances detected. This is not supported.", reference);
			return false;
		}

		Logr.Info("PrinterReferenceWindowContent registered.", reference);
		PrinterReferenceWC = reference;
		return true;
	}

	public bool UnregisterPrinterReference(PrinterReferenceWindowContent reference)
	{
		if (reference == null) return false;
		if (reference != PrinterReferenceWC)
		{
			Logr.Error("Multiple PrinterReferenceWindowContent instances detected. This is not supported.", reference);
			return false;
		}
            
		Logr.Info("PrinterReferenceWindowContent unregistered.", reference);
		PrinterReferenceWC = null;
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