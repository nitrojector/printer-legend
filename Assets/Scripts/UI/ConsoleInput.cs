using UnityEngine.UIElements;

namespace UI.UIDocs
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