/*
using Drafthorse.Helper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Display.Params;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static Drafthorse.Helper.Layout;
using IOComponents;
using Rhino;

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
                Description = "Do not use button to activate - toggle only",
                Optional = true,
            }, ParamRelevance.Secondary),
            new ParamDefinition(new Param_Guid
            {
                Name = "GUID",
                NickName = "G",
                Description = "GUID for Detail Object",
                Optional = false,
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
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Guid
            {
                //This should probably change to the GH Detail type (?)
                Name = "GUID",
                NickName = "G",
                Description = "GUID for Detail Object",
                Optional = false,
            }, ParamRelevance.Binding),
            new ParamDefinition (new Param_ModelDisplayMode
            {
                Name = "Display",
                NickName = "D[]",
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
                NickName = "P[]",
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
          : base("Edit Details", "DetailEdit",
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
        public override GH_Exposure Exposure => GH_Exposure.hidden;
                
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
            if (!TryGetData<bool>(DA, inputs[num++].Param.Name, out var value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            //used for qualified override
            bool targetDefined = false;

            if (!TryGetData<Guid>(DA, inputs[num++].Param.Name, out var value1)) return;
            Guid detailGUID = Guid.Empty;
            if (!value1.HasValue) detailGUID = value1.Value;
            
            Rhino.DocObjects.DetailViewObject detail = RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as Rhino.DocObjects.DetailViewObject; ;
            if (detail == null)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is not valid");
                return;
            }

            Box targetBox = Box.Unset;
            TryGetData<Box>(DA, inputs[num++].Param.Name, out var value2);
            if (value2.HasValue) 
            {
                targetBox = value2.Value;
                targetDefined = true;
            }
            else targetBox = new Box(new BoundingBox(detail.Viewport.CameraTarget, detail.Viewport.CameraTarget));
            BoundingBox targetBBox = targetBox.BoundingBox;
            
            double scale = 1.0;
            TryGetData<double>(DA, inputs[num++].Param.Name, out var value3);
            if (value3.HasValue) scale = value3.Value;
            else scale = detail.DetailGeometry.PageToModelRatio;
                   
            int? pNum = null;
            TryGetData<int>(DA, inputs[num++].Param.Name, out var value4);
            if (value4.HasValue) pNum = value4.Value;
            else pNum = 0;
            
            //Check that Projection is valid
            if (!Enum.IsDefined(typeof(DefinedViewportProjection), pNum))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, pNum + " is not a valid Projection number. Projection will not be modified");
            DefinedViewportProjection projection = (DefinedViewportProjection)pNum;
            
            
            //string dName = string.Empty;
            //if (!DA.GetData("Display", ref dName)) dName = detail.Viewport.DisplayMode.EnglishName;

            //convert LocalName to EnglishName
            //if (ValList.GetDisplaySettingsList(true).Contains(dName))
            //    dName = ValList.GetDisplaySettingsList(false)[ValList.GetDisplaySettingsList(true).IndexOf(dName)];
            
            //if (!ValList.GetDisplaySettingsList(false).Contains(dName))
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, dName + " is not a valid Display Mode name");
             

            ModelDisplayMode dMode = new ModelDisplayMode();
            string dName = dMode.DisplayName;
            TryGetData<ModelDisplayMode>(DA, inputs[num++].Param.Name, out var value5);
            if (value5.IsValid) dName = value5.DisplayName;
            else dName = detail.Viewport.DisplayMode.EnglishName;
            
            DisplayModeDescription displayMode = DisplayModeDescription.FindByName(dName);

            ModelView view = new ModelView();
            TryGetData<ModelView>(DA, inputs[num++].Param.Name, out var value6);
            if (value6.IsValid)
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
            //DA.SetData("GUID", detailGUID);
            TrySetData(DA, outputs[num2++].Param.Name, () => detailGUID);
            //DA.SetData("Display", newDisplayMode);
            TrySetData(DA, outputs[num2++].Param.Name, () => newDisplayMode);
            //DA.SetData("Target", detail.Viewport.CameraTarget);
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.CameraTarget);
            //DA.SetData("Scale", detail.DetailGeometry.PageToModelRatio);
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.DetailGeometry.PageToModelRatio);
            //DA.SetData("Projection", detail.Viewport.Name);
            TrySetData(DA, outputs[num2++].Param.Name, () => detail.Viewport.Name);
            //DA.SetData("View", new ModelView(new Rhino.DocObjects.ViewportInfo(detail.Viewport)));
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
 */