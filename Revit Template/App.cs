using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace RevitTemplate
{
    /// <summary>
    /// This is the main class which defines the Application, and inherits from Revit's
    /// IExternalApplication class.
    /// </summary>
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            // Method to add Tab and Panel 
            RibbonPanel panel = RibbonPanel(a);
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

            // Add ribbon panel and button
            if (panel.AddItem(
                new PushButtonData("Add finishing", "Add finishing", thisAssemblyPath,
                    "RevitTemplate.EntryCommand")) is PushButton button)
            {
                // Defines the tooltip displayed when the button is hovered over in Revit's ribbon
                button.ToolTip = "Add inside walls surface finishing";
                // Defines the icon for the button in Revit's ribbon - note the string formatting
                BitmapImage largeImage = new BitmapImage(
                    new Uri(
                        "pack://application:,,,/RevitTemplate;component/Resources/" +
                        "constrolconstruction_logo_32.png"));
                button.LargeImage = largeImage;
                BitmapImage smallImage = new BitmapImage(
                    new Uri(
                        "pack://application:,,,/RevitTemplate;component/Resources/" +
                        "constrolconstruction_logo_16.png"));
                button.LargeImage = smallImage;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// What to do when the application is shut down.
        /// </summary>
        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }

        public RibbonPanel RibbonPanel(UIControlledApplication a)
        {
            string tab = "ConstrolTest"; // Tab name
            // Empty ribbon panel 
            RibbonPanel ribbonPanel = null;
            // Try to create ribbon tab. 
            try
            {
                a.CreateRibbonTab(tab);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }

            // Try to create ribbon panel.
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, "Constrol test");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }

            // Search existing tab for your panel.
            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels.Where(p => p.Name == "Constrol test"))
            {
                ribbonPanel = p;
            }

            //return panel 
            return ribbonPanel;
        }
    }
}