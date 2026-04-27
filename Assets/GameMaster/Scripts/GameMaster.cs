using System;
using System.Collections.Generic;
using Desktop.WindowSystem;
using Printer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameMaster.Scripts
{
	public class GameMaster : MonoBehaviour
	{
		public static GameMaster Instance { get; private set; }
		
		[SerializeField] private float stateUpdateInterval = 0.25f;
		private float nextUpdateTime = 0f;
		private GameMasterUI ui;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			
			ui = GetComponent<GameMasterUI>();
		}

		private void Update()
		{
			if (ui.Ready && Time.time >= nextUpdateTime)
			{
				UpdateStateStr();
				nextUpdateTime = Time.time + stateUpdateInterval;
			}
		}

		private void UpdateStateStr()
		{
			var view = ui.StateView;
			view.ClearConsole();
			
			{
				var similarity = PrintState.GetSimilarityScore();
				 view.Info($"Similarity: {similarity * 100.0f:0.00}%\n");
			}
		}

		public void Evaluate(string input)
		{
			input = input.Trim();
			List<string> tokens = new(input.Split(' ', StringSplitOptions.RemoveEmptyEntries));

			if (tokens.Count < 1) return;
			
			InfoLn($"[<color=aqua>>></color>] <color=green>{input}</color>");
            
			string cmd = tokens[0].ToLower();
            
			if (string.IsNullOrEmpty(cmd))
				cmd = "help";

			try
			{
				switch (cmd)
				{
					case "help": {
						InfoLn("Available commands:");
						InfoLn("help - <u>Show this help message</u>");
						InfoLn("timescale - <u>Set the game timescale</u>");
						InfoLn("newref - <u>grab new reference image</u>");
						InfoLn("ps - <u>list active windows</u>");
						InfoLn("kill [id] - <u>kill window by id</u>");
						InfoLn("killall - <u>kill all windows</u>");
						InfoLn("clear - <u>clear console</u>");
						break;
					}

					case "timescale":
					{
						if (tokens.Count < 2 || !float.TryParse(tokens[1], out float scale))
						{
							InfoLn("Usage: timescale [scale]");
							break;
						}
                        
						Time.timeScale = scale;
						InfoLn($"Time scale set to {scale}");
						
						break;
					}

					case "newref":
					{
						GameMgr.Instance.PrinterReferenceWC.pReference.LoadRandomReference();
						InfoLn("Loaded new reference image");
						
						break;
					}

					case "ps":
					{
						foreach (var w in WindowManager.Instance.ActiveWindows)
						{
							InfoLn($"{$"[{w.WindowId}]",5} (<color=yellow>{w.Content.GetType().Name}</color>) {w.Title}");
						}
						var count = WindowManager.Instance.ActiveWindows.Count;
						InfoLn($"{count} window{(count != 1 ? "s" : "")}");

						break;
					}

					case "kill":
					{
						if (tokens.Count < 2 || !int.TryParse(tokens[1], out int id))
						{
							InfoLn("Usage: kill [windowId]");
							break;
						}
						
						WindowManager.Instance.Kill(id);
						InfoLn($"Killed window with ID {id}");
						
						break;
					}

					case "killall":
					{
						WindowManager.Instance.KillAll();
						InfoLn("Killed all windows");
						
						break;
					}
					
					case "clear":
					{
						ui.ConsoleView.ClearConsole();
						break;
					}

					default:
					{
						ErrLn($"Unknown command: {cmd}");
						break;
					}
				}
			} catch (Exception e)
			{
				ErrLn($"Error evaluating command: {e.Message}");
				ErrLn(e.StackTrace);
			}
		}
        
		private void InfoLn(string line)
		{
			ui.ConsoleView.Info(line);
		}
		
		private void WarnLn(string line)
		{
			ui.ConsoleView.Warn(line);
		}

		private void ErrLn(string line)
		{
			ui.ConsoleView.Error(line);
		}

		public static bool Instantiate()
		{
			if (Instance != null) return false;
            
			GameObject go = new GameObject("GameMaster");
			var ui = go.AddComponent<UIDocument>();

			var panelSettings = Resources.Load<PanelSettings>("GMPanelPS");
			var uxml = Resources.Load<VisualTreeAsset>("GMPanel");

			if (ui == null) return false;
            
			ui.panelSettings = panelSettings;
			ui.visualTreeAsset = uxml;
            
			go.AddComponent<GameMaster>();
			go.AddComponent<GameMasterUI>();
            
			DontDestroyOnLoad(go);
			return true;
		}
	}
}