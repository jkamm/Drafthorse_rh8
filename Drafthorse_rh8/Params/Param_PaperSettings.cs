using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel.Data;
using System.Linq;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using System.Windows.Forms;
using Drafthorse_rh8.Properties;
using GH_IO.Serialization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Drafthorse_rh8.Properties;

namespace Drafthorse.Params
{
    public class Param_PaperSettings : GH_PersistentParam<PaperSettingsGoo>
    {
        // Track which paper list to show
        private enum PaperListMode
        {
            Default,   // Match Rhino document units
            Metric,    // Show metric papers
            Imperial   // Show imperial papers
        }

        private PaperListMode _currentMode = PaperListMode.Default;

        public Param_PaperSettings() : base("Paper Settings", "Paper Settings",
            "Contains paper configuration settings for making and printing layouts",
            "Drafthorse", "Params")
        { }

        public override Guid ComponentGuid => new Guid("3fe38c3c-f48f-4188-92f4-52051c73adcd");

        protected override Bitmap Icon => Drafthorse_rh8.Properties.Resources.PaperSettingsParam;

        protected override GH_GetterResult Prompt_Plural(ref List<PaperSettingsGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref PaperSettingsGoo value)
        {
            return GH_GetterResult.cancel;
        }

        protected override ToolStripMenuItem Menu_CustomSingleValueItem()
        {
            ToolStripMenuItem customSingle = new ToolStripMenuItem("Set one Paper Setting",Resources.PaperSettingsParam,ShowPaperSizesMenu);            
            return customSingle;
        }

        // To disable the "Set Multiple..." functionality
        //protected override ToolStripMenuItem Menu_CustomMultiValueItem()
        //{
        //    return null;
        //}

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            //Menu_AppendItem(menu, "Units", menu_Units_DoClick,)

            // Add a separator
            Menu_AppendSeparator(menu);

            //// Add paper size selection
            //Menu_AppendItem(menu, "Set One Paper Settings...", ShowPaperSizesMenu, true);

            //// Add a separator
            //Menu_AppendSeparator(menu);

            // Add the three options for unit system
            var label = Menu_AppendItem(menu, "Select Paper Units (changes available paper sizes)");
            label.Enabled = false;            

            // Add the three options for unit system with checkmarks for current mode
            Menu_AppendItem(menu, "Default (Match Rhino)", Default_DoClick, true,
                _currentMode == PaperListMode.Default);
            Menu_AppendItem(menu, "Metric", Metric_DoClick, true,
                _currentMode == PaperListMode.Metric);
            Menu_AppendItem(menu, "Imperial", Imperial_DoClick, true,
                _currentMode == PaperListMode.Imperial);

        }

        private void Imperial_DoClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Switch to Imperial Paper Sizes");
            _currentMode = PaperListMode.Imperial;
            ExpireSolution(true);
        }

        private void Metric_DoClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Switch to Metric Paper Sizes");
            _currentMode = PaperListMode.Metric;
            ExpireSolution(true);
        }

        private void Default_DoClick(object sender, EventArgs e)
        {
            RecordUndoEvent("Switch to Default Paper Sizes");
            _currentMode = PaperListMode.Default;
            ExpireSolution(true);
        }


        private void ShowPaperSizesMenu(object sender, EventArgs e)
        {
            // Create a popup menu for paper sizes
            ContextMenuStrip paperMenu = new ContextMenuStrip();

            // Get appropriate paper list based on current mode
            List<PaperSettings> paperList;

            if (_currentMode == PaperListMode.Default)
            {
                // Use the PaperSettings method to check if Rhino units are metric
                PaperSettings tempSettings = new PaperSettings();
                bool isMetric = tempSettings.RhinoDocPageUnitsAreMetric();
                paperList = isMetric ?
                    PaperSettingsLibrary.GetMetricPapers() :
                    PaperSettingsLibrary.GetImperialPapers();
            }
            else if (_currentMode == PaperListMode.Metric)
            {
                paperList = PaperSettingsLibrary.GetMetricPapers();
            }
            else // Imperial
            {
                paperList = PaperSettingsLibrary.GetImperialPapers();
            }

            // Add each paper size to the menu
            foreach (var paper in paperList)
            {
                var paperItem = paperMenu.Items.Add(paper.ToString());
                var paperSettings = paper; // Capture for closure
                paperItem.Click += (s, args) => {
                    RecordUndoEvent("Set Paper Size");
                    ApplyPaperPreset(paperSettings);
                };
            }

            // Calculate position - typically below the main menu
            Point screenPoint = Grasshopper.Instances.ActiveCanvas.PointToScreen(
                new Point((int)Attributes.Bounds.Left, (int)Attributes.Bounds.Bottom));

            // Show the menu
            paperMenu.Show(screenPoint);
        }

        // Method to apply a paper preset to all values
        // Method to apply a paper preset to all values
        public void ApplyPaperPreset(PaperSettings preset)
        {
            // Don't directly manipulate the data structure
            // Instead, use the SetPersistentData method provided by GH_PersistentParam

            // Record paths before clearing data (keep this part of your code)
            List<GH_Path> existingPaths = new List<GH_Path>(PersistentData.Paths);

            // Create a new structure to hold the updated paper settings
            GH_Structure<PaperSettingsGoo> newData = new GH_Structure<PaperSettingsGoo>();

            // If no existing data, create a new entry at path 0
            if (existingPaths.Count == 0)
            {
                newData.Append(new PaperSettingsGoo(preset.Duplicate()), new GH_Path(0));
            }
            else
            {
                // Add the new preset to all existing paths
                foreach (GH_Path path in existingPaths)
                {
                    newData.Append(new PaperSettingsGoo(preset.Duplicate()), path);
                }
            }

            // Use the proper method to set persistent data
            // This records undo events and handles all the necessary updates
            SetPersistentData(newData);

            // The SetPersistentData method already schedules ExpireSolution(false)
            // But we want to force a full recompute, so:
            ExpireSolution(true);
        }

        public override bool Write(GH_IWriter writer)
        {
            // Save the current mode
            writer.SetInt32("PaperListMode", (int)_currentMode);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            // Restore the mode if it exists
            if (reader.ItemExists("PaperListMode"))
                _currentMode = (PaperListMode)reader.GetInt32("PaperListMode");
            return base.Read(reader);
        }
    }

    // This is the actual data structure containing paper settings
    public class PaperSettingsGoo : GH_Goo<PaperSettings>
    {
        public PaperSettingsGoo() { }
        public PaperSettingsGoo(PaperSettings settings) { Value = settings; }

        public override bool IsValid => Value != null;
        public override string TypeName => "Paper Settings";
        public override string TypeDescription => "Paper configuration settings";

        public override IGH_Goo Duplicate()
        {
            if (Value == null) return new PaperSettingsGoo();
            return new PaperSettingsGoo(Value.Duplicate());
        }              

        public override string ToString()
        {
            if (Value == null) return "Null Paper Settings";
            return Value.ToString();
        }

        public override bool CastFrom(object source)
        {
            // Handle string conversion
            if (source is string paperName)
            {
                try
                {
                    // Use the explicit operator we defined in PaperSettings
                    Value = (PaperSettings)paperName;
                    return true;
                }
                catch (ArgumentException ex)
                {
                    // Pass along the error message about paper name not found
                    //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                    return false;
                }
            }

            // Handle direct PaperSettings object
            if (source is PaperSettings paperSettings)
            {
                Value = paperSettings.Duplicate();
                return true;
            }

            // Handle another PaperSettingsGoo
            if (source is PaperSettingsGoo goo)
            {
                if (goo.Value == null) return false;
                Value = goo.Value.Duplicate();
                return true;
            }

            return false;
        }

        // Add or modify your Value property
        public override PaperSettings Value
        {
            get { return base.Value; }
            set
            {
                // Validate before setting
                if (value != null)
                {
                    if (!value.ValidateDimensions())
                        throw new ArgumentException("Invalid paper dimensions");

                    if (!value.ValidateMargins())
                        throw new ArgumentException("Invalid paper margins");
                }

                base.Value = value;
            }
        }

        //public override bool CastTo<T>(out T target)
        //{
        //    // Allow casting to string
        //    if (typeof(T) == typeof(string) && Value != null)
        //    {
        //        target = (T)(object)Value.ToString();
        //        return true;
        //    }

        //    // Allow casting to PaperSettings
        //    if (typeof(T) == typeof(PaperSettings) && Value != null)
        //    {
        //        target = (T)(object)Value.Duplicate();
        //        return true;
        //    }

        //    target = default;
        //    return false;
        //}

        public override bool Write(GH_IWriter writer)
        {
            if (Value == null) return false;

            // Save all the properties of PaperSettings
            writer.SetString("Name", Value.Name);
            writer.SetDouble("Width", Value.Width);
            writer.SetDouble("Height", Value.Height);
            writer.SetInt32("Unit", (int)Value.Unit);
            writer.SetInt32("Orientation", (int)Value.Orientation);
            writer.SetDouble("MarginTop", Value.MarginTop);
            writer.SetDouble("MarginBottom", Value.MarginBottom);
            writer.SetDouble("MarginLeft", Value.MarginLeft);
            writer.SetDouble("MarginRight", Value.MarginRight);

            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            // Create a new PaperSettings object
            Value = new PaperSettings();

            // Read all the properties
            if (reader.ItemExists("Name")) Value.Name = reader.GetString("Name");
            if (reader.ItemExists("Width")) Value.Width = reader.GetDouble("Width");
            if (reader.ItemExists("Height")) Value.Height = reader.GetDouble("Height");
            if (reader.ItemExists("Unit")) Value.Unit = (PaperUnit)reader.GetInt32("Unit");
            if (reader.ItemExists("Orientation")) Value.Orientation = (PaperOrientation)reader.GetInt32("Orientation");
            if (reader.ItemExists("MarginTop")) Value.MarginTop = reader.GetDouble("MarginTop");
            if (reader.ItemExists("MarginBottom")) Value.MarginBottom = reader.GetDouble("MarginBottom");
            if (reader.ItemExists("MarginLeft")) Value.MarginLeft = reader.GetDouble("MarginLeft");
            if (reader.ItemExists("MarginRight")) Value.MarginRight = reader.GetDouble("MarginRight");

            return true;
        }
    }


}
