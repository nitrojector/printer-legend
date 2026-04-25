using UnityEngine.UIElements;

namespace GameMaster.Scripts.UI
{
    [UxmlElement("ConsoleView", libraryPath = "GM", visibility = LibraryVisibility.Visible)]
    public partial class ConsoleView : VisualElement
    {
        private readonly ScrollView _scrollView;

        public ConsoleView()
        {
            AddToClassList("console-view");

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.AddToClassList("console-view__scroll");
            Add(_scrollView);
        }

        public void Info(string message) => AppendLine(message, "info");
        public void Warn(string message) => AppendLine(message, "warn");
        public void Error(string message) => AppendLine(message, "error");

        public void ClearConsole() => _scrollView.Clear();

        private void AppendLine(string message, string level)
        {
            var line = new Label(message);
            line.AddToClassList("console-view__line");
            line.AddToClassList($"console-view__line--{level}");
            _scrollView.Add(line);
            _scrollView.schedule.Execute(() =>
            {
                if (line.parent != null) _scrollView.ScrollTo(line);
            });
        }
    }
}
