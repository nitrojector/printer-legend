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

	// ── Print view registry ───────────────────────────────────────────────

	private readonly Dictionary<int, PrinterViewWindowContent>      _printViews       = new();
	private readonly Dictionary<int, PrinterReferenceWindowContent> _references       = new();
	private readonly HashSet<int>                                    _progressionIds   = new();
	private int _nextPrintViewId = 1;
	
	/// <summary>
	/// Collection of active print views. IDs are assigned on registration and may not be contiguous.
	/// </summary>
	public IEnumerable<PrinterViewWindowContent> ActivePrintViews => _printViews.Values;
	
	/// <summary>
	/// Collection of active reference windows that are linked to print views.
	/// </summary>
	public IEnumerable<PrinterReferenceWindowContent> ActiveReferences => _references.Values;

	/// <summary>Number of active print views that have a linked reference window.</summary>
	public int ReferenceLinkedPrintViewCount => _references.Count;

	/// <summary>Number of active print views running in progression mode.</summary>
	public int ProgressionLinkedPrintViewCount => _progressionIds.Count;

	// ── Print view registration ───────────────────────────────────────────

	/// <summary>Registers a new print view and returns its assigned ID.</summary>
	public int RegisterPrintView(PrinterViewWindowContent view)
	{
		int id = _nextPrintViewId++;
		_printViews[id] = view;
		Logr.Info($"PrinterView registered (id={id}).", view);
		return id;
	}

	/// <summary>
	/// Unregisters a print view. Closes its linked reference window if one exists.
	/// </summary>
	public void UnregisterPrintView(int id)
	{
		if (!_printViews.Remove(id)) return;
		_progressionIds.Remove(id);

		if (_references.TryGetValue(id, out var refWc))
		{
			_references.Remove(id);
			refWc.Close();
		}

		Logr.Info($"PrinterView unregistered (id={id}).");
	}

	// ── Reference registration ────────────────────────────────────────────

	/// <summary>Links a reference window to an existing print view ID.</summary>
	public void RegisterReference(int printViewId, PrinterReferenceWindowContent reference)
	{
		_references[printViewId] = reference;
		Logr.Info($"PrinterReference registered (printViewId={printViewId}).", reference);
	}

	/// <summary>Unlinks and removes the reference window for a print view.</summary>
	public void UnregisterReference(int printViewId)
	{
		if (_references.Remove(printViewId))
			Logr.Info($"PrinterReference unregistered (printViewId={printViewId}).");
	}

	// ── Query ─────────────────────────────────────────────────────────────

	public PrinterViewWindowContent      GetPrintView(int id) => _printViews.GetValueOrDefault(id);
	public PrinterReferenceWindowContent GetReference(int id) => _references.GetValueOrDefault(id);
	public bool HasPrintView(int id)  => _printViews.ContainsKey(id);
	public bool HasReference(int id)  => _references.ContainsKey(id);

	/// <summary>Marks or unmarks a print view as running in progression mode.</summary>
	public void SetPrintViewProgressionMode(int id, bool isProgression)
	{
		if (isProgression) _progressionIds.Add(id);
		else _progressionIds.Remove(id);
	}

	// ── Players ───────────────────────────────────────────────────────────

	public List<PrinterPlayerController> Players { get; } = new();

	private bool _isPaused = false;

	public bool RegisterPlayer(PrinterPlayerController player)
	{
		if (Players.Contains(player)) return false;
		Players.Add(player);
		return true;
	}

	public bool UnregisterPlayer(PrinterPlayerController player) => Players.Remove(player);

	public void SetPaused(bool paused)
	{
		if (paused == _isPaused) return;
		_isPaused = paused;
		foreach (var p in Players)
			p.SetPaused(paused);
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics() => _instance = null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize() => _instance ??= new GameMgr();
}
