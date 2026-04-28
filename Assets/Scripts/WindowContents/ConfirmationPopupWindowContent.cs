using System;
using Desktop.WindowSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class ConfirmationPopupWindowContent : WindowContent
	{
		public override bool AllowMaximize => false;
		
		public override bool AllowMinimize => false;

		public string Title
		{
			get => WindowTitle;
			set
			{
				_title = value;
				SetTitle(value);
			}
		}

		public string Message
		{
			get => _message;
			set
			{
				_message = value;
				if (messageText != null)
				{
					messageText.text = _message;
				}
			}
		}
		
		public string ConfirmButtonText 
		{
			get => _confirmButtonText;
			set
			{
				_confirmButtonText = value;
				if (confirmButtonText != null)
				{
					confirmButtonText.text = _confirmButtonText;
				}
			}
		}
		
		/// <summary>
		/// Text for the cancel button.
		/// </summary>
		public string CancelButtonText
		{
			get => _cancelButtonText;
			set
			{
				_cancelButtonText = value;
				if (cancelButtonText != null)
				{
					cancelButtonText.text = _cancelButtonText;
				}
			}
		}

		/// <summary>
		/// Callback for when user confirms the confirmation popup.
		/// </summary>
		public event Action OnConfirm;
		
		/// <summary>
		/// Callback for when user cancels the confirmation popup.
		/// </summary>
		public event Action OnCancel;
		
		private string _title = "Confirmation";
		private string _message = "Are you sure?";
		private string _confirmButtonText = "Confirm";
		private string _cancelButtonText = "Cancel";
		private bool _decided;

		[SerializeField] private Button confirmButton;
		[SerializeField] private TMP_Text confirmButtonText;
		[SerializeField] private Button cancelButton;
		[SerializeField] private TMP_Text cancelButtonText;
		[SerializeField] private TMP_Text messageText;
		
		/// <summary>
		/// Set whether the cancel button is shown.
		/// If false, the user will be forced to confirm the confirmation popup.
		/// </summary>
		/// <param name="allowCancel">if cancel is allowed</param>
		public void SetAllowCancel(bool allowCancel)
		{
			cancelButton.gameObject.SetActive(allowCancel);
		}

		public override bool OnQuit()
		{
			if (!_decided)
			{
				if (cancelButton.gameObject.activeSelf)
					OnCancel?.Invoke();
				else
					OnConfirm?.Invoke();
			}
			return true;
		}

		private void Awake()
		{
			SetTitle(_title);
			Message = _message;
			ConfirmButtonText = _confirmButtonText;
			CancelButtonText = _cancelButtonText;

			confirmButton.onClick.AddListener(() =>
			{
				_decided = true;
				OnConfirm?.Invoke();
				CloseWindow();
			});

			cancelButton.onClick.AddListener(() =>
			{
				_decided = true;
				OnCancel?.Invoke();
				CloseWindow();
			});
		}
	}
}