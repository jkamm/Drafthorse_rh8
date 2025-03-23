using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Rhinoceros.Display;
using Rhino;
using Rhino.Geometry;

using Drafthorse.Component.Base;
using Drafthorse.Helper;
using Drafthorse.Helper.PaperSize; // Add this using statement

namespace Drafthorse.Component.Layout
{
    /// <summary>
    /// Creates a new layout page in the active Rhino document
    /// </summary>
    public class LayoutNew_ZUI : ZuiComponent
    {
        private static ParamDefinition[] CreateInputs()
        {
            // Default to inches in case there's no active document
            UnitSystem defaultUnitSystem = UnitSystem.Inches;
            int defaultUnitsParam = 0; // Default to inches (0)
            double defaultWidth = 17;
            double defaultHeight = 11;

            // Try to get the current document's page unit system
            if (RhinoDoc.ActiveDoc != null)
            {
                defaultUnitSystem = RhinoDoc.ActiveDoc.PageUnitSystem;

                // Check if the unit system name ends with "meter" to set metric defaults
                string unitName = defaultUnitSystem.ToString().ToLowerInvariant();
                if (unitName.EndsWith("meters"))
                {
                    // Use millimeters as default for metric
                    defaultUnitsParam = 2; // Millimeters
                    defaultWidth = 420; // A3 width in mm
                    defaultHeight = 297; // A3 height in mm
                }
            }

            return new ParamDefinition[10]  // Increased the array size to 9 for the new parameter
            {
                new ParamDefinition(new Params.Param_BooleanToggle
                {
                    Name = "Run",
                    NickName = "R",
                    Description = "Hide to show 'Make' Button \nDo not use button to activate - toggle only",
                    Optional = true,
                }, ParamRelevance.Primary),
                new ParamDefinition(new Param_String
                {
                    Name = "Name",
                    NickName = "N",
                    Description = "PageName for new layout",
                    Optional = true,
                }, ParamRelevance.Primary),
                // Add the paper size parameter
                ParamDefinition.Create<Param_Integer>("Paper", "P", "Standard paper size\n-1 = Custom (use Width and Height)", -1, GH_ParamAccess.item, true, ParamRelevance.Primary),
                ParamDefinition.Create<Param_Boolean>("Landscape", "L", "True = Landscape, False = Portrait", false, GH_ParamAccess.item, true, ParamRelevance.Primary),
                ParamDefinition.Create<Param_Number>("Width", "W", "Page Width", defaultWidth, GH_ParamAccess.item, false, ParamRelevance.Primary),
                ParamDefinition.Create<Param_Number>("Height", "H", "Page Height", defaultHeight, GH_ParamAccess.item, false, ParamRelevance.Primary),
                ParamDefinition.Create<Param_Integer>("Details", "D", "Details (0-4)", 4, GH_ParamAccess.item, true, ParamRelevance.Secondary),
                new ParamDefinition(new Param_Point
                {
                    Name = "Target",
                    NickName = "T",
                    Description = "Single target for all details on a single layout",
                    Optional = true,
                }, ParamRelevance.Secondary),
                ParamDefinition.Create<Param_Number>("Scale", "S", "Scale for details", 1.0, GH_ParamAccess.item, true, ParamRelevance.Secondary),
                ParamDefinition.Create<Param_Integer>("Units", "U", "Sets Page Units\n\n0 = inches\n1 = centimeters\n2 = millimeters", defaultUnitsParam, GH_ParamAccess.item, false, ParamRelevance.Tertiary)
            };
        }

        private static readonly ParamDefinition[] inputs = CreateInputs();

        private static readonly ParamDefinition[] outputs = new ParamDefinition[1]
        {
            new ParamDefinition(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
            {
                Name = "Page",
                NickName = "P",
                Description = "Layout Page(s) Added to Document",
                Access = GH_ParamAccess.item,
            }, ParamRelevance.Primary)
        };

        protected override ParamDefinition[] Inputs => inputs;
        protected override ParamDefinition[] Outputs => outputs;

        // Field to store the created page for persistence between solutions
        private ModelPageViewport _newPage = null;
        private bool _firstRun = true;

        /// <summary>
        /// Initializes a new instance of the LayoutNew_ZUI class.
        /// </summary>
        public LayoutNew_ZUI()
          : base("Create New Layout ZUI", "NewLayout",
              "Create a new layout from scratch",
              "Drafthorse", "Layout")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region EscapeBehavior
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            // Get basic parameters
            int i = 0;
            bool run = false;
            TryGetData<bool>(DA, inputs[i++].Param.Name, out bool? runValue);
            if (!(runValue == null)) run = runValue.Value;

            string pageName = string.Empty;
            TryGetData(DA, inputs[i++].Param.Name, out pageName);

            // Get paper size selection and orientation
            int PaperSizeIndex = -1;
            TryGetData(DA, inputs[i++].Param.Name, out int? PaperSizeValue);
            if(!(PaperSizeValue == null)) PaperSizeIndex = PaperSizeValue.Value;

            bool landscape = false;
            TryGetData(DA, inputs[i++].Param.Name, out bool? landscapeValue);
            landscape = landscapeValue ?? false;

            // Initialize dimensions based on document units
            UnitSystem docUnitSystem = RhinoDoc.ActiveDoc?.PageUnitSystem ?? UnitSystem.Inches;
            bool isMetric = docUnitSystem.ToString().ToLowerInvariant().EndsWith("meters");

            // Default dimensions based on unit system
            double width = isMetric ? 297 : 11;
            double height = isMetric ? 210 : 8.5;

            // Apply direct width/height inputs if provided
            TryGetData(DA, inputs[i++].Param.Name, out double? widthValue);
            if (!(widthValue == null)) width = widthValue.Value;

            TryGetData(DA, inputs[i++].Param.Name, out double? heightValue);
            if (heightValue.HasValue) height = heightValue.Value;

            // Get paper size list
            List<PaperSize> PaperSizes = isMetric
                ? PaperSizeLibrary.GetMetricPaperSizes()
                : PaperSizeLibrary.GetImperialPaperSizes();

            // Check for direct inputs that would override paper size
            bool hasDirectInputs = false;
            try
            {
                int widthIndex = Params.IndexOfInputParam("Width");
                int heightIndex = Params.IndexOfInputParam("Height");

                if (widthIndex >= 0 && heightIndex >= 0)
                {
                    hasDirectInputs = Params.Input[widthIndex].SourceCount > 0 ||
                                     Params.Input[heightIndex].SourceCount > 0;
                }
            }
            catch (Exception)
            {
                // Silently handle parameter access errors
            }

            // Apply paper size if selected and no direct inputs
            int units = 0; // Default to inches
            if (PaperSizeIndex >= 0 && PaperSizeIndex < PaperSizes.Count && !hasDirectInputs)
            {
                PaperSize selectedSize = PaperSizes[PaperSizeIndex];

                // Apply dimensions based on orientation
                if (landscape)
                {
                    width = Math.Max(selectedSize.Width, selectedSize.Height);
                    height = Math.Min(selectedSize.Width, selectedSize.Height);
                }
                else
                {
                    width = Math.Min(selectedSize.Width, selectedSize.Height);
                    height = Math.Max(selectedSize.Width, selectedSize.Height);
                }

                // Apply units if specified in paper size
                if (selectedSize.Units.HasValue)
                {
                    units = selectedSize.Units.Value;

                    try
                    {
                        int unitsIndex = Params.IndexOfInputParam("Units");
                        if (unitsIndex >= 0 && Params.Input[unitsIndex].SourceCount == 0)
                        {
                            if (Params.Input[unitsIndex] is Param_Integer unitsParam)
                            {
                                unitsParam.PersistentData.Clear();
                                unitsParam.PersistentData.Append(new GH_Integer(units));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Silently handle parameter access errors
                    }
                }
            }

            // Get remaining parameters
            int detailCount = 0;
            TryGetData(DA, inputs[i++].Param.Name, out int? detailsValue);
            detailCount = Math.Min(Math.Max(detailsValue ?? 0, 0), 4);

            Point3d target = new Point3d(0, 0, 0);
            TryGetData<Point3d>(DA, inputs[i++].Param.Name, out var targetValue);
            if (targetValue != null) target = targetValue.Value;

            double scale = 1;
            TryGetData(DA, inputs[i++].Param.Name, out double? scaleValue);
            scale = scaleValue ?? 1.0;

            // Get or use default units
            TryGetData(DA, inputs[i++].Param.Name, out int? unitsValue);
            if (unitsValue.HasValue) units = unitsValue.Value;

            UnitSystem pageUnits = units == 2 ? UnitSystem.Millimeters :
                                  units == 1 ? UnitSystem.Centimeters :
                                              UnitSystem.Inches;

            // Validate parameters
            if (width <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Width must be greater than zero");
                return;
            }

            if (height <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Height must be greater than zero");
                return;
            }

            if (scale <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale must be greater than zero");
                return;
            }

            // Add information messages
            if (hasDirectInputs && PaperSizeIndex >= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Paper size preset is being overridden by direct width/height inputs.");
            }

            // Generate default name if needed
            if (string.IsNullOrEmpty(pageName))
            {
                pageName = "New Layout (" + detailCount.ToString() + " details)";
            }

            // Create the layout if triggered
            bool pressed = (base.Attributes as CustomAttributes)?.Pressed ?? false;
            if (run || pressed)
            {
                RhinoDoc.ActiveDoc.AdjustPageUnitSystem(pageUnits, false);

                try
                {
                    Tuple<bool, string> layoutResult;

                    layoutResult = Helper.Layout.AddNewLayout(pageName, width, height, target, detailCount, scale, out Rhino.Display.RhinoPageView newLayout);

                    if (!layoutResult.Item1)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, layoutResult.Item2);
                        return;
                    }

                    if (newLayout != null)
                    {
                        _newPage = new ModelPageViewport(newLayout.MainViewport);
                    }
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error creating layout: " + ex.Message);
                }
            }

            // Output the result
            TrySetData(DA, outputs[0].Param.Name, () => _newPage);
        }

        /// <summary>
        /// Called when variable parameters need maintenance such as adding named values to dropdown lists
        /// </summary>
        public override void VariableParameterMaintenance()
        {
            base.VariableParameterMaintenance();

            // Add named values to the Units parameter
            int unitsIndex = base.Params.IndexOfInputParam("Units");
            if (unitsIndex >= 0 && base.Params.Input[unitsIndex] is Param_Integer param)
            {
                param.ClearNamedValues();
                param.AddNamedValue("Inches", 0);
                param.AddNamedValue("Centimeters", 1);
                param.AddNamedValue("Millimeters", 2);
            }

            // Add paper sizes to the Paper parameter
            int paperIndex = base.Params.IndexOfInputParam("Paper");
            if (paperIndex >= 0 && base.Params.Input[paperIndex] is Param_Integer paperParam)
            {
                paperParam.ClearNamedValues();
                paperParam.AddNamedValue("Custom", -1);

                // Get the appropriate paper size list based on current unit system
                UnitSystem unitSystem = RhinoDoc.ActiveDoc?.PageUnitSystem ?? UnitSystem.Inches;
                string unitName = unitSystem.ToString().ToLowerInvariant();
                bool isMetric = unitName.EndsWith("meters");

                List<PaperSize> PaperSizes = isMetric ?
                    PaperSizeLibrary.GetMetricPaperSizes() :
                    PaperSizeLibrary.GetImperialPaperSizes();

                // Add each paper size to the dropdown
                for (int i = 0; i < PaperSizes.Count; i++)
                {
                    paperParam.AddNamedValue(PaperSizes[i].Name, i);
                }
            }
        }

        /// <summary>
        /// Add portrait/landscape toggle to the component menu
        /// </summary>
        //protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        //{
        //    base.AppendAdditionalComponentMenuItems(menu);

        //    Menu_AppendSeparator(menu);
        //    Menu_AppendItem(menu, "Toggle Portrait/Landscape", OnToggleOrientation, true, false);
        //}

        /// <summary>
        /// Toggle between portrait and landscape orientation
        /// </summary>
        //private void OnToggleOrientation(object sender, EventArgs e)
        //{
        //    int widthIndex = Params.IndexOfInputParam("Width");
        //    int heightIndex = Params.IndexOfInputParam("Height");

        //    // Start with debug messages
        //    RhinoApp.WriteLine("Toggle orientation requested");
        //    RhinoApp.WriteLine($"Width index: {widthIndex}, Height index: {heightIndex}");

        //    if (widthIndex >= 0 && heightIndex >= 0)
        //    {
        //        double width = 0;
        //        double height = 0;

        //        // Declare parameters outside the if statements
        //        Param_Number widthParam = null;
        //        Param_Number heightParam = null;

        //        // Check if we can cast to Param_Number and log result
        //        if (Params.Input[widthIndex] is Param_Number tempWidthParam)
        //        {
        //            widthParam = tempWidthParam;
        //            RhinoApp.WriteLine("Width parameter found and is Param_Number");

        //            // Try to access persistent data safely and log intermediate steps
        //            try
        //            {
        //                RhinoApp.WriteLine($"Width param has {widthParam.PersistentData.PathCount} paths");
        //                if (widthParam.PersistentData.PathCount > 0)
        //                {
        //                    Grasshopper.Kernel.Data.GH_Path path = widthParam.PersistentData.get_Path(0);
        //                    RhinoApp.WriteLine($"Path 0 has length {path.Length}");

        //                    int itemCount = widthParam.PersistentData.get_Branch(path).Count;
        //                    RhinoApp.WriteLine($"Branch has {itemCount} items");

        //                    if (itemCount > 0)
        //                    {
        //                        object item = widthParam.PersistentData.get_FirstItem(true);
        //                        RhinoApp.WriteLine($"First item type: {(item != null ? item.GetType().Name : "null")}");

        //                        if (item is GH_Number widthGoo)
        //                        {
        //                            width = widthGoo.Value;
        //                            RhinoApp.WriteLine($"Width value: {width}");
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                RhinoApp.WriteLine($"Error accessing width data: {ex.Message}");
        //            }
        //        }
        //        else
        //        {
        //            RhinoApp.WriteLine("Width parameter is not a Param_Number");
        //        }

        //        // Do the same for height
        //        if (Params.Input[heightIndex] is Param_Number tempHeightParam)
        //        {
        //            heightParam = tempHeightParam;
        //            RhinoApp.WriteLine("Height parameter found and is Param_Number");

        //            try
        //            {
        //                RhinoApp.WriteLine($"Height param has {heightParam.PersistentData.PathCount} paths");
        //                if (heightParam.PersistentData.PathCount > 0)
        //                {
        //                    Grasshopper.Kernel.Data.GH_Path path = heightParam.PersistentData.get_Path(0);
        //                    RhinoApp.WriteLine($"Path 0 has length {path.Length}");

        //                    int itemCount = heightParam.PersistentData.get_Branch(path).Count;
        //                    RhinoApp.WriteLine($"Branch has {itemCount} items");

        //                    if (itemCount > 0)
        //                    {
        //                        object item = heightParam.PersistentData.get_FirstItem(true);
        //                        RhinoApp.WriteLine($"First item type: {(item != null ? item.GetType().Name : "null")}");

        //                        if (item is GH_Number heightGoo)
        //                        {
        //                            height = heightGoo.Value;
        //                            RhinoApp.WriteLine($"Height value: {height}");
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                RhinoApp.WriteLine($"Error accessing height data: {ex.Message}");
        //            }
        //        }
        //        else
        //        {
        //            RhinoApp.WriteLine("Height parameter is not a Param_Number");
        //        }

        //        // Swap width and height if we have valid values
        //        RhinoApp.WriteLine($"Final width: {width}, height: {height}");
        //        RhinoApp.WriteLine($"Width param null? {widthParam == null}, Height param null? {heightParam == null}");

        //        if (width > 0 && height > 0 && widthParam != null && heightParam != null)
        //        {
        //            try
        //            {
        //                RhinoApp.WriteLine("Attempting to swap values");

        //                widthParam.PersistentData.Clear();
        //                widthParam.PersistentData.Append(new GH_Number(height));
        //                RhinoApp.WriteLine("Width parameter updated");

        //                heightParam.PersistentData.Clear();
        //                heightParam.PersistentData.Append(new GH_Number(width));
        //                RhinoApp.WriteLine("Height parameter updated");

        //                RhinoApp.WriteLine("Expiring solution");
        //                ExpireSolution(true);
        //                RhinoApp.WriteLine("Solution expired");
        //            }
        //            catch (Exception ex)
        //            {
        //                RhinoApp.WriteLine($"Error swapping values: {ex.Message}");
        //            }
        //        }
        //        else
        //        {
        //            RhinoApp.WriteLine("Cannot swap values - width/height invalid or parameters null");
        //        }
        //    }
        //    else
        //    {
        //        RhinoApp.WriteLine("Width or height parameter not found");
        //    }
        //}

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.NewLayout;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B45D5FE8-3A51-4E2F-94D2-78A91A47BEF6"); }
        }

        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        private class CustomAttributes : ExpireButtonAttributes
        {
            private new LayoutNew_ZUI Owner => base.Owner as LayoutNew_ZUI;

            public CustomAttributes(ZuiComponent owner)
                : base(owner)
            {
            }

            protected override string DisplayText
            {
                get
                {
                    return "Make";
                }
            }

            protected override bool Visible
            {
                get
                {
                    if (Owner.Params.IndexOfInputParam("Run") >= 0)
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Reset persistent results when component is reset/recomputed from scratch
        /// </summary>
        public override void ClearData()
        {
            base.ClearData();
            _newPage = null;
            _firstRun = true;
        }
    }
}