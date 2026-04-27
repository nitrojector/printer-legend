using System;
using Desktop.WindowSystem;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
	public class ConfirmationPopupWindowContent : WindowContent
	{
		public override string WindowTitle { get; protected set; } = "Confirmation";

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
		public Action OnConfirm;
		
		/// <summary>
		/// Callback for when user cancels the confirmation popup.
		/// </summary>
		public Action OnCancel;
		
		private string _message = "Are you sure?";
		private string _confirmText = "Confirm";
		private string _cancelText = "Cancel";

		[SerializeField] private Button confirmButton;
		[SerializeField] private TMP_Text confirmButtonText;
		[SerializeField] private Button cancelButton;
		[SerializeField] private TMP_Text cancelButtonText;
		[SerializeField] private TMP_Text messageText;

		private void Awake()
		{
			WindowTitle = Title;
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