using UnityEngine.UIElements;

namespace UI.UIDocs
{
    [UxmlElement("ConsoleView", libraryPath = "GM", visibility = LibraryVisibility.Visible)]
    public partial class ConsoleView : Label
    {
        public ConsoleView()
        {
            AddToClassList("console-view");
        }
    }
}