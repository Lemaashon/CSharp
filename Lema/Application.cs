using Lema.Commands;
using Nice3point.Revit.Toolkit.External;

namespace Lema
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "Lema");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/Lema;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/Lema;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}