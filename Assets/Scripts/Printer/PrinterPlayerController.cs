using System;
using EngineSystem;
using Gallery;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Printer
{
	[RequireComponent(typeof(PrintheadController))]
	[RequireComponent(typeof(PrinterMagic))]
	public class PrinterPlayerController : MonoBehaviour
	{
		[field: Header("Auto Cursor Advance")]
		[field: SerializeField, Min(0.001f)]
		public float PrintStepsPerSecond { get; set; } = 10f;
        
		[Header("References")]
		[SerializeField] private GameObject startGamePrompt;
		[SerializeField] private TMP_Text coundownText;

		/// <summary>
		/// Whether player is paused so no input propagate
		/// </summary>
		private bool _isPlayerPaused = false;
        
		/// <summary>
		/// Whether the player is currently printing
		/// </summary>
		private bool printingStarted = false;

		private bool completionTriggered = false;

		private bool printingCountdownActive = false;

		private int _resetCount = 0;
		private float _printStartTime = 0f;

		/// <summary>
		/// Count down before print starts
		/// </summary>
		private float startTimer = 0f;

		private const float StartTimeDelay = 3.0f;

		[SerializeField] private LevelManager levelManager;

		private PrintheadController printhead;
		private PrinterMagic        magic;

		/// <summary>Print Activation Action</summary>
		private InputAction printAction;

		/// <summary>Line Feed Action</summary>
		private InputAction lfAction;

		/// <summary>Carriage Return Action</summary>
		private InputAction crAction;

		/// <summary>Speed Adjust Action</summary>
		[Obsolete]
		private InputAction speedAction;

		private InputAction speed0Action;
		private InputAction speed1Action;
		private InputAction speed2Action;
        
		// Color channel actions (one per palette entry)
		private InputAction color1Action;
		private InputAction color2Action;
		private InputAction color3Action;
		private InputAction color4Action;

		private InputAction resetAction;

		// Stored delegates so subscribe/unsubscribe are symmetrical
		private readonly Action<InputAction.CallbackContext>[] colorHoldDelegates    = new Action<InputAction.CallbackContext>[4];
		private readonly Action<InputAction.CallbackContext>[] colorReleaseDelegates = new Action<InputAction.CallbackContext>[4];

		private float cursorStepTimer;
		
		private readonly Action<InputAction.CallbackContext>[] speedDelegates = new Action<InputAction.CallbackContext>[3];

		private void Awake()
		{
			printhead = GetComponent<PrintheadController>();
			magic     = GetComponent<PrinterMagic>();
			
			printAction  = InputSystem.actions["Printer/Print"];
			lfAction     = InputSystem.actions["Printer/LF"];
			crAction     = InputSystem.actions["Printer/CR"];
			speedAction  = InputSystem.actions["Printer/Speed"];
			color1Action = InputSystem.actions["Printer/Color1"];
			color2Action = InputSystem.actions["Printer/Color2"];
			color3Action = InputSystem.actions["Printer/Color3"];
			color4Action = InputSystem.actions["Printer/Color4"];
			speed0Action = InputSystem.actions["Printer/Speed0"];
			speed1Action = InputSystem.actions["Printer/Speed1"];
			speed2Action = InputSystem.actions["Printer/Speed2"];
			resetAction = InputSystem.actions["UI/Reset"];

			// Build per-index delegates once; lambdas capture the loop variable correctly
			for (int i = 0; i < 4; i++)
			{
				int idx = i;
				colorHoldDelegates[idx]    = _ => OnColorHold(idx);
				colorReleaseDelegates[idx] = _ => OnColorRelease(idx);
			}

			startGamePrompt.SetActive(true);
			coundownText.gameObject.SetActive(false);
            
			GameMgr.Instance.RegisterPlayer(this);
		}

		private void OnEnable()
		{
			// RegisterInput();
		}

		private void OnDisable()
		{
			UnregisterInput();
		}
        
		private void RegisterInput()
		{
			lfAction.performed += OnLF;
			crAction.performed += OnCR;
			resetAction.performed += OnReset;

			SubscribeColorActions(true);
			SubscribeSpeedActions(true);
		}


		private void UnregisterInput()
		{
			lfAction.performed -= OnLF;
			crAction.performed -= OnCR;
			resetAction.performed -= OnReset;

			SubscribeColorActions(false);
			SubscribeSpeedActions(false);

			// Release any held channels so magic state stays clean
			for (int i = 0; i < 4; i++)
				magic.ReleaseColor(i);
		}

		private void OnDestroy()
		{
			GameMgr.Instance.UnregisterPlayer(this);
		}

		private void Update()
		{
			if (_isPlayerPaused) return;
                
			if (!printingStarted && printAction.IsPressed())
			{
				printingStarted = true;
				printingCountdownActive = true;
				startGamePrompt.SetActive(false);
                
				startTimer = StartTimeDelay;
				coundownText.SetText($"{Mathf.CeilToInt(startTimer)}");
				coundownText.gameObject.SetActive(true);
				return;
			}

			if (printingCountdownActive)
			{
				coundownText.SetText($"{Mathf.CeilToInt(startTimer)}");
				startTimer -= Time.deltaTime;
				if (startTimer <= 0f)
				{
					printingCountdownActive = false;
					_printStartTime = Time.time;
					RegisterInput();
					coundownText.gameObject.SetActive(false);
				}

				return;
			}

			if (!printingStarted) return;

			if (printhead.IsComplete)
			{
				if (!completionTriggered)
				{
					completionTriggered = true;
					SavePrint();
					SceneManager.LoadScene("FinishPrinting", LoadSceneMode.Single);
				}
				return;
			}

			

			// ── Auto cursor advance (always on) ───────────────────────────────
			cursorStepTimer += Time.deltaTime;
			float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);
			while (cursorStepTimer >= stepInterval)
			{
				printhead.AdvancePrinthead();
				cursorStepTimer -= stepInterval;
			}

			// ── Print: action binding + LMB ───────────────────────────────────
			bool isPrinting = printAction != null && printAction.IsPressed();
			if (isPrinting)
				printhead.Print();

			// Color: always sync — black when no channels are active
			printhead.InkColor = magic.CurrentInkColor;
		}

		// ── Action callbacks ──────────────────────────────────────────────────
		
		private void OnReset(InputAction.CallbackContext ctx)
		{
			_resetCount++;
			completionTriggered = false;
			printingStarted = false;
			printhead.ResetCanvasAndPrinthead();
			coundownText.gameObject.SetActive(false);
			startGamePrompt.SetActive(true);
			UnregisterInput();
		}

		private void OnLF(InputAction.CallbackContext ctx)
		{
			if (!magic.IsAbilityEnabled(PrinterAbility.Newline)) return;
			printhead.AdvanceLine();
		}

		private void OnCR(InputAction.CallbackContext ctx)
		{
			if (!magic.IsAbilityEnabled(PrinterAbility.CarriageReturn)) return;
			printhead.CarriageReturn();
		}

		// Color hold/release — wired to color action performed/canceled
		private void OnColorHold(int index)
		{
			PrinterAbility? ability = index switch
			{
				0 => PrinterAbility.ColorRed,
				1 => PrinterAbility.ColorGreen,
				2 => PrinterAbility.ColorBlue,
				_ => null,
			};
			if (ability.HasValue && magic.IsAbilityEnabled(ability.Value))
				magic.HoldColor(index);
		}

		private void OnColorRelease(int index)
		{
			// Always release regardless of ability state to prevent stuck channels
			magic.ReleaseColor(index);
		}

		// ── Helpers ───────────────────────────────────────────────────────────

		private void SubscribeSpeedActions(bool subscribe)
		{
			InputAction[] actions = { speed0Action, speed1Action, speed2Action };

			for (int i = 0; i < actions.Length; i++)
			{
				if (actions[i] == null) continue;
				int level = i;

				if (subscribe)
				{
					speedDelegates[level] = ctx =>
					{
						if (magic.IsAbilityEnabled(SpeedAbilityForLevel(level)))
							PrintStepsPerSecond = magic.GetSpeedForLevel(level);
					};
					actions[i].performed += speedDelegates[level];
				}
				else
				{
					if (speedDelegates[level] != null)
						actions[i].performed -= speedDelegates[level];
				}
			}
		}

		private void SubscribeColorActions(bool subscribe)
		{
			InputAction[] actions = { color1Action, color2Action, color3Action, color4Action };
			for (int i = 0; i < actions.Length; i++)
			{
				if (actions[i] == null) continue;
				if (subscribe)
				{
					actions[i].performed += colorHoldDelegates[i];
					actions[i].canceled  += colorReleaseDelegates[i];
				}
				else
				{
					actions[i].performed -= colorHoldDelegates[i];
					actions[i].canceled  -= colorReleaseDelegates[i];
				}
			}
		}

		private static PrinterAbility SpeedAbilityForLevel(int level) => level switch
		{
			0 => PrinterAbility.SpeedSlow,
			1 => PrinterAbility.SpeedNormal,
			_ => PrinterAbility.SpeedFast,
		};

		private void SavePrint()
		{
			float duration = Time.time - _printStartTime;
			string refPath = BuildReferencePath();
			GalleryManager.SaveEntry(printhead.Canvas.GetTexture(), refPath, (float)PrintState.GetSimilarityScore(), _resetCount, duration);
		}

		private string BuildReferencePath()
		{
			var sprite = levelManager != null ? levelManager.Reference?.ReferenceSprite : null;
			if (sprite == null) return string.Empty;
			return GalleryManager.MakeInternalPath("PrintRefs/" + sprite.name);
		}

		public bool SetPaused(bool paused)
		{
			if (paused == _isPlayerPaused) return false;
			if (!enabled) return false;
            
			_isPlayerPaused = paused;
			if (_isPlayerPaused)
			{
				UnregisterInput();
			}
			else
			{
				RegisterInput();
			}

			return true;
		}
	}
}