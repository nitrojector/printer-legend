using Desktop.WindowSystem;
using UnityEngine;
using UnityEngine.UI;

namespace WindowContents
{
    public class PrintFinalImageWindowContent : WindowContent
    {
        public override string WindowTitle => "Your Print";
        public override bool AllowMaximize => false;
        public override bool AllowMinimize => false;

        [SerializeField] private RawImage printDisplay;

        private Texture2D _texture;

        public void SetPrintTexture(Texture2D tex) => _texture = tex;

        public override void OnShow()
        {
            if (printDisplay != null && _texture != null)
                printDisplay.texture = _texture;
        }
    }
}
