using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Rhinoceros.Display;
using Rhino;
using Rhino.Geometry;

using Drafthorse.Component.Base;
using Drafthorse.Helper;

namespace Drafthorse.Component.Layout.List2
{
    public class LayoutNew_ZUI : ZuiComponent
    {
        //private static readonly ParamDefinition[] inputs = new ParamDefinition[8]
        //{
        //    new ParamDefinition(new Params.Param_BooleanToggle
        //    {
        //        Name = "Run",
        //        NickName = "R",
        //        Description = "Hide to show 'Make' Button \nDo not use button to activate - toggle only",
        //        Optional = true,
        //    }, ParamRelevance.Primary),
        //    new ParamDefinition(new Param_String
        //    {
        //        Name = "Name",
        //        NickName = "N",
        //        Description = "PageName for new layout",
        //        Optional = true,
        //    }, ParamRelevance.Primary),
        //    //new ParamDefinition(new Param_Number
        //    //{
        //    //    Name = "Height",
        //    //    NickName = "H",
        //    //    Description = "Page Height",
        //    //    Optional = false,
        //    //}, ParamRelevance.Primary),
        //    ParamDefinition.Create<Param_Number>("Height", "H", "Page Height", 11.0, GH_ParamAccess.item, false, ParamRelevance.Primary),
        //    //new ParamDefinition(new Param_Number
        //    //{
        //    //    Name = "Width",
        //    //    NickName = "W",
        //    //    Description = "Page Width",
        //    //    Optional = false,
        //    //}, ParamRelevance.Primary),
        //    ParamDefinition.Create<Param_Number>("Width", "W", "Page Width", 17.0, GH_ParamAccess.item, false, ParamRelevance.Primary),
        //    new ParamDefinition(new Param_Integer
        //    {
        //        Name = "Details",
        //        NickName = "D",
        //        Description = "Details (0-4)",
        //        Optional = true,
        //    }, ParamRelevance.Secondary),
        //    new ParamDefinition(new Param_Point
        //    {
        //        Name = "Target",
        //        NickName = "T",
        //        Description = "Single target for all details on a single layout",
        //        Optional = true,
        //    }, ParamRelevance.Secondary),
        //    new ParamDefinition(new Param_Number
        //    {
        //        Name = "Scale",
        //        NickName = "S",
        //        Description = "Scale for details",
        //        Optional = true,
        //    }, ParamRelevance.Secondary),
        //    //new ParamDefinition(new Param_Integer
        //    //{
        //    //    Name = "Units",
        //    //    NickName = "U",
        //    //    Description = "Sets Page Units\n\n0 = inches\n1 = centimeters\n2 = millimeters",
        //    //    Optional = false,
        //    //}, ParamRelevance.Tertiary),
        //    ParamDefinition.Create<Param_Integer>("Units!", "U", "Sets Page Units\n\n0 = inches\n1 = centimeters\n2 = millimeters",
        //       0, GH_ParamAccess.item, true, ParamRelevance.Primary),
        //};

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

            return new ParamDefinition[8]
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
        ParamDefinition.Create<Param_Number>("Height", "H", "Page Height", defaultHeight, GH_ParamAccess.item, false, ParamRelevance.Primary),
        ParamDefinition.Create<Param_Number>("Width", "W", "Page Width", defaultWidth, GH_ParamAccess.item, false, ParamRelevance.Primary),
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
        private ModelPageViewport _newPage;

        /// <summary>
        /// Initializes a new instance of the LayoutNew_ZUI class.
        /// </summary>
        public LayoutNew_ZUI()
          : base("Create New Layout ZUI", "NewLayout",
              "Create a new layout from scratch",
              "Drafthorse", "Layout")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        private bool _firstRun = true;

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

            if (_firstRun && Rhino.RhinoDoc.ActiveDoc != null)
            {
                UnitSystem pageUnitSystem = RhinoDoc.ActiveDoc.PageUnitSystem;
                _firstRun = false;
            }

            int i = 0;
            bool run = false;
            if (!TryGetData<bool>(DA, inputs[i++].Param.Name, out bool? value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            string pageName = string.Empty;
            TryGetData(DA, inputs[i++].Param.Name, out pageName);

            double height = 11;
            if (!TryGetData(DA, inputs[i++].Param.Name, out double? value1)) return;
            if (value1.HasValue) height = value1.Value;

            double width = 17;
            if (!TryGetData(DA, inputs[i++].Param.Name, out double? value2)) return;
            if (value2.HasValue) width = value2.Value;

            int detailCount = 0;
            TryGetData(DA, inputs[i++].Param.Name, out int? value3);
            if (value3.HasValue) detailCount = value3.Value;
            detailCount = Math.Min(detailCount, 4);
            detailCount = Math.Max(detailCount, 0);

            Point3d target = new Point3d(0, 0, 0);
            TryGetData<Point3d>(DA, inputs[i++].Param.Name, out var value4);
            if (value4 != null) target = value4.Value;

            double scale = 1;
            TryGetData(DA, inputs[i++].Param.Name, out double? value5);
            if (value5.HasValue) scale = value5.Value;
            if (scale <= 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale Error: Scale cannot be less than or equal to 0");

            int units = 0;
            TryGetData(DA, inputs[i++].Param.Name, out int? value6);
            if (value6.HasValue) units = value6.Value;
            Rhino.UnitSystem pageUnits = units == 2 ? Rhino.UnitSystem.Millimeters :
                                        units == 1 ? Rhino.UnitSystem.Centimeters :
                                        Rhino.UnitSystem.Inches;

            // If pageName is still empty, generate a default name
            if (string.IsNullOrEmpty(pageName))
            {
                pageName = "New Layout (" + detailCount.ToString() + " details)";
            }

            bool pressed = (base.Attributes as CustomAttributes).Pressed;

            if (run || pressed)
            {
                Rhino.RhinoDoc.ActiveDoc.AdjustPageUnitSystem(pageUnits, false);

                Tuple<bool, string> layoutResult;
                Rhino.Display.RhinoPageView newLayout = null;

                layoutResult = Helper.Layout.AddNewLayout(pageName, width, height, target, detailCount, scale, out newLayout);

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

            TrySetData(DA, outputs[0].Param.Name, () => _newPage);
        }

        public override void VariableParameterMaintenance()
        {
            base.VariableParameterMaintenance();

            int unitsIndex = base.Params.IndexOfInputParam("Units");
            if (unitsIndex >= 0 && base.Params.Input[unitsIndex] is Param_Integer param)
            {
                param.ClearNamedValues();
                param.AddNamedValue("Inches", 0);
                param.AddNamedValue("Centimeters", 1);
                param.AddNamedValue("Millimeters", 2);
            }
        }

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

        // Reset persistent results when component is reset/recomputed from scratch
        public override void ClearData()
        {
            base.ClearData();
            _newPage = null;
        }
    }
}