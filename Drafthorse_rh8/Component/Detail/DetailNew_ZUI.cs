using Drafthorse.Helper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Display.Params;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static Drafthorse.Helper.Layout;
using Drafthorse.Component.Base;
using Rhino;
using Rhino.Commands;

namespace Drafthorse.Component.Detail
{
    public class DetailNew_ZUI : ZuiComponent
    {
        private static readonly ParamDefinition[] inputs = new ParamDefinition[8]
        {
            new ParamDefinition(new Params.Param_BooleanToggle
            {
                Name = "Run",
                NickName = "R",
                Description = "Hide to show 'Make' Button",
                Optional = true,
            }, ParamRelevance.Secondary),
            new ParamDefinition (new Param_ModelPageViewport
            {
                Name = "Layout Page",
                NickName = "P",
                Description = "Layout Page to add detail",
                Access = GH_ParamAccess.item
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_Rectangle
            {
                Name = "Bounds",
                NickName = "B",
                Description = "Detail Boundary Rectangle on Layout Page",
                Access = GH_ParamAccess.item
            }, ParamRelevance.Primary),
            new ParamDefinition (new Param_ModelDisplayMode
            {
                Name = "Display",
                NickName = "D[]",
                Description = "Model Display Mode\nAttach Value List for list of Display Modes",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Box
            {
                Name = "Target",
                NickName = "T",
                Description = "Target for Detail\nPoint is acceptable input for Parallel Views\nOverrides View",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Number
            {
                Name = "Scale",
                NickName = "S",
                Description = "Page Units per Model Unit",
                Optional = true,                
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Integer
            {
                Name = "Projection",
                NickName = "P[]",
                Description = "View Projection \nAttach Value List for list of projections",
                Optional = true,                
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_ModelView
            {
                Name = "View",
                NickName = "V",
                Description = "Model View",
                Optional = true,
                Hidden = true,
            }, ParamRelevance.Primary),
        };

        private static readonly ParamDefinition[] outputs = new ParamDefinition[7]
        {
            new ParamDefinition(new Param_String
            {
                Name = "Result",
                NickName = "R",
                Description = "Success or Failure for each detail",
                Optional = true,
            }, ParamRelevance.Secondary),
            new ParamDefinition (new Param_GenericObject
            {
                Name = "Detail",
                NickName = "Dt",
                Description = "Referenced Detail Object",
                Access = GH_ParamAccess.item
            }, ParamRelevance.Binding),
            new ParamDefinition (new Param_ModelDisplayMode
            {
                Name = "Display",
                NickName = "D",
                Description = "Model Display Mode",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Point
            {
                Name = "Target",
                NickName = "T",
                Description = "Camera Target for Detail",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Number
            {
                Name = "Scale",
                NickName = "S",
                Description = "Page Units per Model Unit",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_String
            {
                Name = "Projection",
                NickName = "P",
                Description = "ViewPort Viewname",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_ModelView
            {
                Name = "View",
                NickName = "V",
                Description = "Detail View",
                Optional = true,
                Hidden = true,
            }, ParamRelevance.Primary),
        };

        protected override ParamDefinition[] Inputs => inputs;
        protected override ParamDefinition[] Outputs => outputs;

        /// <summary>
        /// Initializes a new instance of the DetailNew_ZUI class.
        /// </summary>
        public DetailNew_ZUI()
          : base("New Detail 2", "NewDetail",
              "Add a new detail to an existing layout",
              "Drafthorse", "Detail")
        {
            Hidden = true;
        }

        public override void VariableParameterMaintenance()
        {
            base.VariableParameterMaintenance();
            int num = base.Params.IndexOfInputParam("Projection");
            if (num >= 0 && base.Params.Input[num] is Param_Integer param_Integer)
            {
                param_Integer.AddNamedValue("None", 0);
                param_Integer.AddNamedValue("Top", 1);
                param_Integer.AddNamedValue("Bottom", 2);
                param_Integer.AddNamedValue("Left", 3);
                param_Integer.AddNamedValue("Right", 4);
                param_Integer.AddNamedValue("Front", 5);
                param_Integer.AddNamedValue("Back", 6);
                param_Integer.AddNamedValue("Perspective", 7);
                param_Integer.AddNamedValue("Two-Point Perspective", 8);
            };
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region EscapeBehavior
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            //used for qualified override
            bool targetDefined = false;
            //bool scaleDefined = false;
            //bool projectionDefined = false;
            //bool viewDefined = false;

            int num = 0;
            bool run = false;
            if (!TryGetData(DA, inputs[num++].Param.Name, out bool? value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            if (!TryGetData(DA, inputs[num++].Param.Name, out ModelPageViewport page)) return;
            if (page.PageNumber == null) return;
            RhinoPageView pageView = GetPage((int)page.PageNumber);

            if (!TryGetData(DA, inputs[num++].Param.Name, out Rectangle3d? value1)) return;
            Rectangle3d dBounds = value1.Value;

            ModelDisplayMode dMode = new ModelDisplayMode();
            TryGetData<ModelDisplayMode>(DA, inputs[num++].Param.Name, out var value2);
            if (!(value2 == null)) dMode = value2.DisplayName;
            string dName = dMode.DisplayName;
            if (dName == null) dName = DisplayModeDescription.FindByName("Wireframe").EnglishName;
            DisplayModeDescription displayMode = DisplayModeDescription.FindByName(dName);


            Box targetBox = Box.Empty;
            targetDefined = TryGetData(DA, inputs[num++].Param.Name, out Box? value3);
            if (value3.HasValue) targetBox = value3.Value;
            BoundingBox targetBBox = targetBox.BoundingBox;

            double scale = 1.0;
            TryGetData(DA, inputs[num++].Param.Name, out double? value4);
            if (value4.HasValue) scale = value4.Value;

            int pNum = 0;
            TryGetData(DA, inputs[num++].Param.Name, out int? value5);
            if (value5.HasValue) pNum = value5.Value;
            if (!Enum.IsDefined(typeof(DefinedViewportProjection), pNum))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, pNum + " is not a valid Projection number. Projection will not be modified");
            }
            DefinedViewportProjection projection = (DefinedViewportProjection)pNum;


            ModelView view = new ModelView();
            TryGetData<ModelView>(DA, inputs[num++].Param.Name, out var value6);
            if (value6 != null)
            {
                view = value6;
                if (!targetDefined) targetBBox = new BoundingBox(view.ToViewportInfo().TargetPoint, view.ToViewportInfo().TargetPoint);
            }
            else
            {
                view = new ModelView(new Rhino.DocObjects.ViewportInfo(RhinoDoc.ActiveDoc.Views.GetStandardRhinoViews()[0].MainViewport));
                view.ToViewportInfo().TargetPoint = targetBBox.IsValid ? targetBBox.Center : view.ToViewportInfo().TargetPoint;
                switch (projection)
                {
                    case DefinedViewportProjection.Perspective:
                        {
                            view.ToViewportInfo().ChangeToPerspectiveProjection(10, true, 50);
                            break;
                        }
                    case DefinedViewportProjection.TwoPointPerspective:
                        {
                            view.ToViewportInfo().ChangeToTwoPointPerspectiveProjection(10, Vector3d.ZAxis, 50);
                            break;
                        }
                    default:
                        {
                            view.ToViewportInfo().ChangeToParallelProjection(true);
                            break;
                        }
                }
            }

            // initialize result
            Result result = Result.Failure;
            bool pressed = (base.Attributes as CustomAttributes).Pressed;

            if (pressed || run)
            {
                if (pageView != null)
                {
                    Rectangle3d pageBounds = new Rectangle3d(Plane.WorldXY, pageView.PageWidth, pageView.PageHeight);
                    bool upperLeft = pageBounds.Contains(dBounds.Corner(3)) == PointContainment.Inside;
                    bool lowerRight = pageBounds.Contains(dBounds.Corner(1)) == PointContainment.Inside;
                    if (!upperLeft || !lowerRight)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "New detail is (atleast partially) outside of layout boundaries");

                    var detail = pageView.AddDetailView("ModelView", new Point2d(dBounds.Corner(3)), new Point2d(dBounds.Corner(1)), projection);
                    pageView.SetPageAsActive();
                    RhinoDoc.ActiveDoc.Views.ActiveView = pageView;
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    if (detail != null)
                    {
                        result = ReviseDetail(detail, targetBBox, scale, projection, displayMode, view.ToViewportInfo());
                    }

                    ModelView newView = new ModelView(new Rhino.DocObjects.ViewportInfo(detail.Viewport));
                    ModelDisplayMode newDisplayMode = new ModelDisplayMode(displayMode);
                    GH_DetailView newDetail = new GH_DetailView(detail.Id);

                    int num2 = 0;
                    TrySetData(DA, outputs[num2++].Param.Name, () => result);
                    TrySetData(DA, outputs[num2++].Param.Name, () => newDetail);
                    TrySetData(DA, outputs[num2++].Param.Name, () => newDisplayMode);
                    TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.CameraTarget);
                    TrySetData(DA, outputs[num2++].Param.Name, () => detail.DetailGeometry.PageToModelRatio);
                    TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.Name);
                    TrySetData(DA, outputs[num2++].Param.Name, () => newView);
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutNewDetail_bitmap;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9624f60c-774b-4229-96e6-b0672a64d3ba"); }
        }

        #region Add Value Lists
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Add List of Views", Menu_ViewClick);
            Menu_AppendItem(menu, "Add List of DisplayModes", Menu_DisplayClick);
        }

        private void Menu_ViewClick(object sender, EventArgs e)
        {
            int num = Params.IndexOfInputParam("Projection");
            if (num >= 0)
            {
                string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
                List<string> projNames = pNames.Select(v => v.ToString()).ToList();
                List<DefinedViewportProjection> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).ToList();
                List<string> projVals = pVals.Select(c => ((int)c).ToString()).ToList();

                if (!ValList.AddOrUpdateValueList(this, num, "Views", "Pick Projection: ", projNames, projVals))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + num.ToString() + "] failed to update");
                ExpireSolution(true);
            }
        }

        private void Menu_DisplayClick(object sender, EventArgs e)
        {
            int num = Params.IndexOfInputParam("Display");
            if (num >= 0)
            {
                List<string> dNames = ValList.GetDisplaySettingsList(false);
                List<string> dLocalNames = ValList.GetDisplaySettingsList(true);
                if (!ValList.AddOrUpdateValueList(this, num, "Display", "Pick Display: ", dLocalNames, dNames))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + num.ToString() + "] failed to update");
                ExpireSolution(true);
            }
        }
        #endregion Add Value Lists

        #region AutoValueList
        private bool _handled = false;

        private void SetupEventHandlers()
        {
            if (_handled)
                return;

            int pageInput = Params.IndexOfInputParam("Layout Page");
            int displayInput = Params.IndexOfInputParam("Display");
            int projectionInput = Params.IndexOfInputParam("Projection");

            if (pageInput >= 0) Params.Input[pageInput].ObjectChanged += InputParamChanged;
            if (displayInput >= 0) Params.Input[displayInput].ObjectChanged += InputParamChanged;
            if (projectionInput >= 0) Params.Input[projectionInput].ObjectChanged += InputParamChanged;

            _handled = true;
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            SetupEventHandlers();
        }

        public void InputParamChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            int pageInput = Params.IndexOfInputParam("Layout Page");
            int displayInput = Params.IndexOfInputParam("Display");
            int projectionInput = Params.IndexOfInputParam("Projection");

            if (pageInput >= 0 && sender.NickName == Params.Input[pageInput].NickName)
            {
                var pageDictionary = Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews().ToDictionary(v => v.PageName, v => v.PageNumber);
                List<string> pageViewNames = pageDictionary.Keys.ToList();
                List<string> layoutIndices = new List<string>();
                for (int i = 0; i < pageViewNames.Count; i++)
                    layoutIndices.Add(pageDictionary[pageViewNames[i]].ToString());
                try
                {
                    ValList.UpdateValueList(this, pageInput, "Layouts", "Pick Layout(s): ", pageViewNames, layoutIndices);
                    ExpireSolution(true);
                }
                catch (Exception) { };
            }

            if (projectionInput >= 0 && sender.NickName == Params.Input[projectionInput].NickName)
            {
                string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
                List<string> projNames = pNames.Select(v => v.ToString()).ToList();
                List<DefinedViewportProjection> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).ToList();
                List<string> projVals = pVals.Select(c => ((int)c).ToString()).ToList();
                try
                {
                    ValList.UpdateValueList(this, projectionInput, "Views", "Pick Projection: ", projNames, projVals);
                    ExpireSolution(true);
                }
                catch (Exception) { };
            }

            if (displayInput >= 0 && sender.NickName == Params.Input[displayInput].NickName)
            {
                List<string> displayNames = ValList.GetDisplaySettingsList(true);
                List<string> displayVals = ValList.GetDisplaySettingsList(false);
                try
                {
                    ValList.UpdateValueList(this, displayInput, "Display", "Pick Display: ", displayNames, displayVals);
                    ExpireSolution(true);
                }
                catch (Exception) { };
            }
        }
        #endregion AutoValueList

        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        private class CustomAttributes : ExpireButtonAttributes
        {
            private new DetailNew_ZUI Owner => base.Owner as DetailNew_ZUI;

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
    }
}