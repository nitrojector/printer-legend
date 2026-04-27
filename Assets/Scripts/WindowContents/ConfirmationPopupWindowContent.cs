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
			set => SetTitle(value);
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
		
		public string ConfirmText 
		{
			get => _confirmText;
			set
			{
				_confirmText = value;
				if (confirmButtonText != null)
				{
					confirmButtonText.text = _confirmText;
				}
			}
		}
		
		/// <summary>
		/// Text for the cancel button.
		/// </summary>
		public string CancelText
		{
			get => _cancelText;
			set
			{
				_cancelText = value;
				if (cancelButtonText != null)
				{
					cancelButtonText.text = _cancelText;
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
		
		private string _message = "Are you sure?";
		private string _confirmText = "Confirm";
		private string _cancelText = "Cancel";

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
			// quit is cancel if exists, otherwise confirm
			if (cancelButton.gameObject.activeSelf)
			{
				OnCancel?.Invoke();
			}
			else
			{
				OnConfirm?.Invoke();
			}
			return true;
		}

		private void Awake()
		{
			SetTitle("Confirmation");
			Message = _message;
			ConfirmText = _confirmText;
			CancelText = _cancelText;
			
			confirmButton.onClick.AddListener(() =>
			{
				OnConfirm?.Invoke();
				CloseWindow();
			});
			
			cancelButton.onClick.AddListener(() =>
			{
				OnCancel?.Invoke();
				CloseWindow();
			});
		}
	}
}