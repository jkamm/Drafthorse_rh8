
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
//using System.Xml.Schema;

namespace Drafthorse.Component.Detail
{
    public class DetailEdit_ZUI : ZuiComponent
    {
        private static readonly ParamDefinition[] inputs = new ParamDefinition[7]
        {
            new ParamDefinition(new Params.Param_BooleanToggle
            {
                Name = "Run",
                NickName = "R",
                Description = "Hide to show 'Modify' Button",
                Optional = true,
            }, ParamRelevance.Secondary),
            new ParamDefinition (new Param_DetailView
            {
                Name = "Detail",
                NickName = "Dt",
                Description = "Detail Object",
                //Optional = false,
                Access = GH_ParamAccess.item
            }, ParamRelevance.Binding),
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
                Description = "Page Units per Model Unit\nOverrides Target to set scale\nOverrides View",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Integer
            {
                Name = "Projection", 
                NickName = "P[]", 
                Description = "View Projection \nAttach Value List for list of projections\nOverrides View",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_ModelView
            {
                Name = "View",
                NickName = "V",
                Description = "Model View\nGood for Named and Perspective Views",
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
            //new ParamDefinition(new Param_Guid
            //{
            //    //This should probably change to the GH Detail type (?)
            //    Name = "GUID",
            //    NickName = "G",
            //    Description = "GUID for Detail Object",
            //    Optional = false,
            //}, ParamRelevance.Binding),
            new ParamDefinition (new Param_DetailView
            {
                Name = "Detail",
                NickName = "Dt",
                Description = "Detail Object",
                Optional = false,
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
                Description = "Camera Target",
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
                NickName = "P",
                Description = "View Projection",
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
        /// Initializes a new instance of the DetailEdit_ZUI class.
        /// </summary>
        public DetailEdit_ZUI()
          : base("Edit Details 2", "DetailEdit",
              "Modify detail views in a layout",
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
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region EscapeBehavior
            //Esc behavior code snippet from 
            // http://james-ramsden.com/you-should-be-implementing-esc-behaviour-in-your-grasshopper-development/
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            int num = 0;

            bool run = false;
            if (!TryGetData<bool>(DA, inputs[num++].Param.Name, out bool? value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            //used for qualified override
            bool targetDefined = false;

            Guid detailGUID = Guid.Empty;
            if (!TryGetData<GH_DetailView>(DA, inputs[num++].Param.Name, out GH_DetailView value1)) return;

            if (value1.IsValid) detailGUID = value1.ReferenceID;
            
            Rhino.DocObjects.DetailViewObject detail = RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as Rhino.DocObjects.DetailViewObject; ;
            if (detail == null)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is not valid");
                return;
            }

            ModelDisplayMode dMode = new ModelDisplayMode();
            string dName = dMode.DisplayName;
            TryGetData<ModelDisplayMode>(DA, inputs[num++].Param.Name, out var value2);
            if (!(value2 == null)) dName = value2.DisplayName;
            else dName = detail.Viewport.DisplayMode.EnglishName;

            DisplayModeDescription displayMode = DisplayModeDescription.GetDisplayModes().FirstOrDefault(mode => mode.DisplayAttributes.EnglishName == dName);


            Box targetBox = Box.Unset;
            TryGetData<Box>(DA, inputs[num++].Param.Name, out Box? value3);
            if (value3.HasValue) 
            {
                targetBox = value3.Value;
                targetDefined = true;
            }
            else targetBox = new Box(new BoundingBox(detail.Viewport.CameraTarget, detail.Viewport.CameraTarget));
            BoundingBox targetBBox = targetBox.BoundingBox;
            
            double scale = 1.0;
            TryGetData<double>(DA, inputs[num++].Param.Name, out double? value4);
            value4 = value4?? 1.0;
            if (value3.HasValue) scale = value4.Value;
            else scale = detail.DetailGeometry.PageToModelRatio;
            scale *= Rhino.RhinoMath.UnitScale(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem, Rhino.RhinoDoc.ActiveDoc.PageUnitSystem);

            int pNum;
            TryGetData<int>(DA, inputs[num++].Param.Name, out int? value5);
            if (value5.HasValue) pNum = value5.Value;
            else pNum = 0;
            
            //Check that Projection is valid
            if (!Enum.IsDefined(typeof(DefinedViewportProjection), pNum))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, pNum + " is not a valid Projection number. Projection will not be modified");
            DefinedViewportProjection projection = (DefinedViewportProjection)pNum;
                       
            
            ModelView view = new ModelView();
            TryGetData<ModelView>(DA, inputs[num++].Param.Name, out var value6);
            if (value6 != null)
            {
                view = value6;
                if (!targetDefined) targetBBox = new BoundingBox(view.ToViewportInfo().TargetPoint, view.ToViewportInfo().TargetPoint);
            }
            else view = new ModelView(new Rhino.DocObjects.ViewportInfo(detail.Viewport));

            bool pressed = (base.Attributes as CustomAttributes).Pressed;

            Rhino.Commands.Result detailResult = Rhino.Commands.Result.Nothing;
            if (pressed || run)
            {
                detailResult = ReviseDetail(detail, targetBBox, scale, projection, displayMode, view.ToViewportInfo());
            }
            
            ModelDisplayMode newDisplayMode = new ModelDisplayMode(displayMode);

            int num2 = 0;
            TrySetData(DA, outputs[num2++].Param.Name, () => detailResult);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => detailGUID);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => newDisplayMode);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.CameraTarget);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.DetailGeometry.PageToModelRatio);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.Name);
            
            TrySetData(DA, outputs[num2++].Param.Name, () => new ModelView(new Rhino.DocObjects.ViewportInfo(detail.Viewport)));
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutDetail_bitmap;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2bee9799-c07b-4baa-b4e1-bf930a03885b"); }
        }

        #region Add Value Lists
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            
            Menu_AppendItem(menu, "Add List of Display Modes", Menu_DisplayClick);
            Menu_AppendItem(menu, "Add List of Projections", Menu_ViewClick);
        }

        private void Menu_ViewClick(object sender, EventArgs e)
        {
            int num = Params.IndexOfInputParam("Projection");
            if (num >= 0)
            {
                string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
                List<string> projNames = pNames.Select(v => v.ToString()).ToList();
                List<int> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).Select(c => (int)c).ToList();
                List<string> projVals = pVals.ConvertAll(v => v.ToString());

                if (!ValList.AddOrUpdateValueList(this, num, "Views", "Pick Projection: ", projNames, projVals))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + num.ToString() + "] failed to update");
            }

            ExpireSolution(true);
        }

        private void Menu_DisplayClick(object sender, EventArgs e)
        {

            int num = Params.IndexOfInputParam("Display");
            if (num >= 0)
            {
                List<string> dNames = ValList.GetDisplaySettingsList(false);
                List<string> dLocalNames = ValList.GetDisplaySettingsList(true);

                if (!ValList.AddOrUpdateValueList(this, num, "Display Mode", "Pick DisplayMode: ", dLocalNames, dNames))
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + num.ToString() + "] failed to update");
            }

            ExpireSolution(true);
        }
        #endregion Add Value Lists

        #region AutoValueList

        //Update a value list if added to a given input(based on Elefront and FabTools)
        //on event for a source added to a given input

        private bool _handled = false;

        private void SetupEventHandlers()
        {
            if (_handled)
                return;

            int num = Params.IndexOfInputParam("Projection");
            int num2 = Params.IndexOfInputParam("Display");
            if (num >= 0) Params.Input[num].ObjectChanged += InputParamChanged;
            if (num2 >= 0) Params.Input[num2].ObjectChanged += InputParamChanged;

            _handled = true;
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            SetupEventHandlers();
        }


        public void InputParamChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            int input1 = Params.IndexOfInputParam("Projection");
            int input2 = Params.IndexOfInputParam("Display");
            if (input1>=0 && sender.NickName == Params.Input[input1].NickName)
            {
                // optional feedback
                // Rhino.RhinoApp.WriteLine("This is the right input");

                //List<string> standardViewNames = ValList.GetStandardViewList();
                string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
                List<string> projNames = pNames.Select(v => v.ToString()).ToList();
                List<int> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).Select(c => (int)c).ToList();
                List<string> projVals = pVals.ConvertAll(v => v.ToString());

                //try to modify input as a valuelist
                try
                {
                    ValList.UpdateValueList(this, input1, "Views", "Pick Projection: ", projNames, projVals);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
                catch (Exception) { };
            }
             
            else if (input2>=0 && sender.NickName == Params.Input[input2].NickName)
            { 
                List<string> displayNames = ValList.GetDisplaySettingsList(true);
                List<string> displayVals = ValList.GetDisplaySettingsList(false);

                //try to modify input as a valuelist
                try
                {
                    ValList.UpdateValueList(this, input2, "Display", "Pick Display: ", displayNames, displayVals);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
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
            private new DetailEdit_ZUI Owner => base.Owner as DetailEdit_ZUI;
            public CustomAttributes(ZuiComponent owner)
            : base(owner)
            {
            }
            protected override string DisplayText
            {
                get
                {
                    return "Modify";
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
