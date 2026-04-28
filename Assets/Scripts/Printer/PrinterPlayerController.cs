using System;
using Data;
using Desktop.WindowSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utility;
using WindowContents;

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

		private bool _paused;
		private bool printingStarted = false;
		private bool completionTriggered = false;
		private bool printingCountdownActive = false;
		private float startTimer = 0f;
		private float _printStartTime = 0f;

		private const float StartTimeDelay = 3.0f;

		/// <summary>Fires once when the printhead reaches its last line.</summary>
		public event Action OnPrintComplete;

		/// <summary>Fires when the active speed level changes (0=slow, 1=normal, 2=fast).</summary>
		public event Action<int> OnSpeedLevelChanged;

		/// <summary>Current speed level (0=slow, 1=normal, 2=fast). Defaults to 1.</summary>
		public int CurrentSpeedLevel { get; private set; } = 1;

		/// <summary>Number of times the player reset during this session.</summary>
		public int RestartCount { get; private set; }

		/// <summary>
		/// Elapsed seconds from the end of the countdown to print completion.
		/// Valid only after <see cref="OnPrintComplete"/> has fired.
		/// </summary>
		public float PrintDuration { get; private set; }

		private PrintheadController printhead;
		private PrinterMagic        magic;

		private InputAction printAction;
		private InputAction lfAction;
		private InputAction crAction;
		private InputAction speed0Action;
		private InputAction speed1Action;
		private InputAction speed2Action;
		private InputAction color1Action;
		private InputAction color2Action;
		private InputAction color3Action;
		private InputAction color4Action;
		private InputAction resetAction;

		private readonly Action<InputAction.CallbackContext>[] colorHoldDelegates    = new Action<InputAction.CallbackContext>[4];
		private readonly Action<InputAction.CallbackContext>[] colorReleaseDelegates = new Action<InputAction.CallbackContext>[4];
		private readonly Action<InputAction.CallbackContext>[] speedDelegates        = new Action<InputAction.CallbackContext>[3];

		private float cursorStepTimer;

		private void Awake()
		{
			printhead = GetComponent<PrintheadController>();
			magic     = GetComponent<PrinterMagic>();
			PrintStepsPerSecond = magic.GetSpeedForLevel(1);

			printAction  = InputSystem.actions["Printer/Print"];
			lfAction     = InputSystem.actions["Printer/LF"];
			crAction     = InputSystem.actions["Printer/CR"];
			color1Action = InputSystem.actions["Printer/Color1"];
			color2Action = InputSystem.actions["Printer/Color2"];
			color3Action = InputSystem.actions["Printer/Color3"];
			color4Action = InputSystem.actions["Printer/Color4"];
			speed0Action = InputSystem.actions["Printer/Speed0"];
			speed1Action = InputSystem.actions["Printer/Speed1"];
			speed2Action = InputSystem.actions["Printer/Speed2"];
			resetAction  = InputSystem.actions["UI/Reset"];

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

		private void OnEnable() { }

		private void OnDisable()
		{
			UnregisterInput();
		}

		private void OnDestroy()
		{
			GameMgr.Instance.UnregisterPlayer(this);
		}

		private void RegisterInput()
		{
			lfAction.performed      += OnLF;
			crAction.performed      += OnCR;
			resetAction.performed   += OnReset;
			SubscribeColorActions(true);
			SubscribeSpeedActions(true);
		}

		private void UnregisterInput()
		{
			lfAction.performed      -= OnLF;
			crAction.performed      -= OnCR;
			resetAction.performed   -= OnReset;
			SubscribeColorActions(false);
			SubscribeSpeedActions(false);
			for (int i = 0; i < 4; i++)
				magic.ReleaseColor(i);
		}

		private void Update()
		{
			if (_paused) return;

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
					PrintDuration = Time.time - _printStartTime;
					OnPrintComplete?.Invoke();
				}
				return;
			}

			// ── Auto cursor advance ───────────────────────────────────────────
			cursorStepTimer += Time.deltaTime;
			float stepInterval = 1f / Mathf.Max(0.001f, PrintStepsPerSecond);
			while (cursorStepTimer >= stepInterval)
			{
				printhead.AdvancePrinthead();
				cursorStepTimer -= stepInterval;
			}

			// ── Print input ───────────────────────────────────────────────────
			if (printAction != null && printAction.IsPressed())
				printhead.Print();

			printhead.InkColor = magic.CurrentInkColor;
		}

		// ── Action callbacks ──────────────────────────────────────────────────

		private void OnReset(InputAction.CallbackContext ctx)
		{
			if (UserSettings.Instance.QuickReset)
			{
				ExecuteReset();
				return;
			}

			SetPaused(true);
			WindowManager.Instance.Launch<ConfirmationPopupWindowContent>((w, c) =>
			{
				w.SetPositionNormalized(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
				c.Title             = "Reset Print?";
				c.Message           = "This will clear your current print. Are you sure?";
				c.ConfirmButtonText = "Reset";
				c.CancelButtonText  = "Continue";
				c.OnConfirm += ExecuteReset;
				c.OnCancel  += ResumeWithCountdown;
			});
		}

		private void ResumeWithCountdown()
		{
			_paused                 = false;
			printingCountdownActive = true;
			startTimer              = StartTimeDelay;
			coundownText.SetText($"{Mathf.CeilToInt(startTimer)}");
			coundownText.gameObject.SetActive(true);
		}

		private void ExecuteReset()
		{
			_paused = false;
			RestartCount++;
			completionTriggered = false;
			printingStarted = false;
			printingCountdownActive = false;
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
						if (!magic.IsAbilityEnabled(SpeedAbilityForLevel(level))) return;
						PrintStepsPerSecond = magic.GetSpeedForLevel(level);
						CurrentSpeedLevel   = level;
						OnSpeedLevelChanged?.Invoke(level);
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

		public bool SetPaused(bool paused)
		{
			if (paused == _paused) return false;
			if (!enabled) return false;
			_paused = paused;
			if (_paused) UnregisterInput();
			else RegisterInput();
			return true;
		}
	}
}
