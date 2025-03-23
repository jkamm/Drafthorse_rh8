using System;
using Grasshopper.Kernel;
using Drafthorse.Params;
using Drafthorse_rh8.Properties;

namespace Drafthorse.Component.Test
{
    public class PaperSettingsComponent : GH_Component
    {
        public PaperSettingsComponent()
            : base("Paper Settings", "Paper",
                   "Configure paper settings for printing or layout",
                   "Drafthorse", "Test")
        { }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("19DEF959-2FBC-4AA2-ACC0-FED85B2734EA"); }
        }

        protected override System.Drawing.Bitmap Icon => Resources.PaperSettings;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Add input for existing paper settings
            pManager.AddParameter(new Param_PaperSettings(), "Input Settings", "Ps", "Input paper settings to modify (optional)", GH_ParamAccess.item);

            // Default values will be used if not connected
            pManager.AddTextParameter("Name", "N", "Settings Name", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Paper width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Paper height", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Unit", "U", "Paper unit (0=in, 1=mm, 2=cm)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Orientation", "O", "Orientation (0=portrait, 1=landscape)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Top Margin", "TM", "Top margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bottom Margin", "BM", "Bottom margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Left Margin", "LM", "Left margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Right Margin", "RM", "Right margin", GH_ParamAccess.item);

            // Make all inputs optional
            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Output the modified settings
            pManager.AddParameter(new Param_PaperSettings(), "Paper Settings", "Ps", "Modified paper settings", GH_ParamAccess.item);

            // Add individual output parameters for all settings
            pManager.AddTextParameter("Name", "N", "Settings Name", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Paper width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Paper height", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Unit", "U", "Paper unit (0=in, 1=mm, 2=cm)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Orientation", "O", "Orientation (0=portrait, 1=landscape)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Top Margin", "TM", "Top margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bottom Margin", "BM", "Bottom margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Left Margin", "LM", "Left margin", GH_ParamAccess.item);
            pManager.AddNumberParameter("Right Margin", "RM", "Right margin", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Content Width", "CW", "Content width (paper width minus margins)", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Content Height", "CH", "Content height (paper height minus margins)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Default settings
            PaperSettings settings = new PaperSettings();

            // Try to get input settings first
            PaperSettingsGoo inputSettingsGoo = null;
            if (DA.GetData(0, ref inputSettingsGoo) && inputSettingsGoo != null)
            {
                settings = inputSettingsGoo.Value.Duplicate();
            }

            // Override with any provided values
            string name = null;
            if (DA.GetData("Name", ref name) && !string.IsNullOrEmpty(name))
                settings.Name = name;

            double width = double.NaN;
            if (DA.GetData("Width", ref width) && !double.IsNaN(width))
                settings.Width = width;

            double height = double.NaN;
            if (DA.GetData("Height", ref height) && !double.IsNaN(height))
                settings.Height = height;

            int unitInt = -1;
            if (DA.GetData("Unit", ref unitInt) && unitInt >= 0 && unitInt <=2)
                settings.Unit = (PaperUnit)unitInt;

            int orientationInt = -1;
            if (DA.GetData("Orientation", ref orientationInt) && orientationInt >= 0)
                settings.Orientation = (PaperOrientation)orientationInt;

            double marginTop = double.NaN;
            if (DA.GetData("Top Margin", ref marginTop) && !double.IsNaN(marginTop))
                settings.MarginTop = marginTop;

            double marginBottom = double.NaN;
            if (DA.GetData("Bottom Margin", ref marginBottom) && !double.IsNaN(marginBottom))
                settings.MarginBottom = marginBottom;

            double marginLeft = double.NaN;
            if (DA.GetData("Left Margin", ref marginLeft) && !double.IsNaN(marginLeft))
                settings.MarginLeft = marginLeft;

            double marginRight = double.NaN;
            if (DA.GetData("Right Margin", ref marginRight) && !double.IsNaN(marginRight))
                settings.MarginRight = marginRight;

            //// Calculate content dimensions
            //double contentWidth = settings.Width - settings.MarginLeft - settings.MarginRight;
            //double contentHeight = settings.Height - settings.MarginTop - settings.MarginBottom;

            // Output the modified settings object
            DA.SetData(0, new PaperSettingsGoo(settings));

            // Output individual properties
            DA.SetData(1, settings.Name);
            DA.SetData(2, settings.Width);
            DA.SetData(3, settings.Height);
            DA.SetData(4, (int)settings.Unit);
            DA.SetData(5, (int)settings.Orientation);
            DA.SetData(6, settings.MarginTop);
            DA.SetData(7, settings.MarginBottom);
            DA.SetData(8, settings.MarginLeft);
            DA.SetData(9, settings.MarginRight);
            //DA.SetData(10, contentWidth);
            //DA.SetData(11, contentHeight);
        }
    }
}