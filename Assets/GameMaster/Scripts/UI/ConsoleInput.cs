using UnityEngine.UIElements;

namespace GameMaster.Scripts.UI
{
    [UxmlElement("ConsoleInput", libraryPath = "GM", visibility = LibraryVisibility.Visible)]
    public partial class ConsoleInput : Label
    {
        public ConsoleInput()
        {
            AddToClassList("console-input");
        }
    }
}