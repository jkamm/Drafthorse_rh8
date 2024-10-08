﻿using Drafthorse.Helper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static Drafthorse.Helper.Layout;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Display.Params;
using Grasshopper.Kernel.Types;

namespace Drafthorse.Component.Detail
{
    public class DetailNew : Base.DH_ButtonComponent
    {
        //Goal: Change from instantiator to separate Detail settings so that a detail can either be modified or baked

        /// <summary>
        /// Initializes a new instance of the AddDetail class.
        /// </summary>
        public DetailNew()
          : base("New Detail", "NewDetail",
              "Add a new detail to an existing layout",
              "Drafthorse", "Detail")
        {
            ButtonName = "Make";
        }
        
        //This hides the component from view!  is it callable?  Don't know.
        public override GH_Exposure Exposure => GH_Exposure.primary;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var bToggle = new Params.Param_BooleanToggle();
            Params.Input[pManager.AddParameter(bToggle, "Run", "R", "run using an input", GH_ParamAccess.item)].Optional = true;
            pManager.AddParameter(new Param_ModelPageViewport(), "Layout Page", "P", "Layout Page to add detail", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Bounds", "B", "Detail Boundary Rectangle on Layout Page", GH_ParamAccess.item);
            pManager.AddParameter(new Param_ModelDisplayMode(), "Display", "D[]", "Model Display Mode\nAttach Value List for list of Display Modes", GH_ParamAccess.item);
            pManager.AddBoxParameter("Target", "T", "Target for Detail\nPoint is acceptable input for Parallel Views\nOverrides View", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale", "S", "Page Units per Model Unit", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Projection", "P[]", "View Projection \nAttach Value List for list of projections", GH_ParamAccess.item, 0);

            var viewParam = new Param_ModelView();
            viewParam.Hidden = true;
            pManager.AddParameter(viewParam, "View", "V", "Model View", GH_ParamAccess.item);

            Params.Input[3].Optional = true;
            Params.Input[4].Optional = true;
            Params.Input[5].Optional = true;
            Params.Input[6].Optional = true;
            Params.Input[7].Optional = true;

            Param_Integer obj = (Param_Integer)pManager[6];
            obj.AddNamedValue("None", 0);
            obj.AddNamedValue("Top", 1);
            obj.AddNamedValue("Bottom", 2);
            obj.AddNamedValue("Left", 3);
            obj.AddNamedValue("Right", 4);
            obj.AddNamedValue("Front", 5);
            obj.AddNamedValue("Back", 6);
            obj.AddNamedValue("Perspective", 7);
            obj.AddNamedValue("Two-Point Perspective", 8);

            //Add attributes: Name, Layer, etc?
            //attributes: Name, Layer, Space OR existing detail object (goal).
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Success or Failure for each detail", GH_ParamAccess.item);
           
            pManager.AddGenericParameter("Detail", "D", "Referenced Detail Object", GH_ParamAccess.item);
                       
            pManager.AddParameter(new Param_ModelDisplayMode(), "Display", "D", "Model Display Mode", GH_ParamAccess.item);

            pManager.AddPointParameter("Target", "T", "Camera Target for Detail", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale", "S", "Page Units per Model Unit", GH_ParamAccess.item);
            pManager.AddTextParameter("Projection", "P", "ViewPort Viewname", GH_ParamAccess.item);

            var viewParam = new Param_ModelView();
            viewParam.Hidden = true;
            pManager.AddParameter(viewParam, "View", "V", "Detail View", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //used for qualified override
            bool targetDefined = false;
            bool scaleDefined = false;
            bool projectionDefined = false;
            //bool viewDefined = false;
            
            bool run = false;
            DA.GetData("Run", ref run);


            ModelPageViewport page = new ModelPageViewport();
            DA.GetData("Layout Page", ref page);

            //int index = new int();
            //DA.GetData("Index", ref index);
            if (page.PageNumber == null) return;

            RhinoPageView pageView = GetPage((int)page.PageNumber);

            Rectangle3d dBounds = new Rectangle3d();
            DA.GetData("Bounds", ref dBounds);

            /*
            Point3d target = new Point3d();
            DA.GetData("Target", ref target);
             */
            Box targetBox = Box.Empty;
            if (DA.GetData("Target", ref targetBox)) targetDefined = true;
            BoundingBox targetBBox = targetBox.BoundingBox;

            double scale = 1.0;
            if (DA.GetData("Scale", ref scale)) scaleDefined = true;
            else scale = 1.0;

            int pNum = 0;
            if (DA.GetData("Projection", ref pNum)) projectionDefined = true;

            if (!Enum.IsDefined(typeof(DefinedViewportProjection), pNum))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, pNum + " is not a valid Projection number. Projection will not be modified");
            }

            DefinedViewportProjection projection = (DefinedViewportProjection)pNum;

            ModelDisplayMode dMode = new ModelDisplayMode();
            DA.GetData("Display", ref dMode);

            string dName = dMode.DisplayName;
            if (dName == null) dName = DisplayModeDescription.FindByName("Wireframe").EnglishName;

            DisplayModeDescription displayMode = DisplayModeDescription.FindByName(dName);

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            ModelView view = new ModelView();
            if (DA.GetData("View", ref view))
            { 
                
                //viewDefined = true;
                if (!targetDefined) targetBBox = new BoundingBox(view.ToViewportInfo().TargetPoint, view.ToViewportInfo().TargetPoint);
                if (!scaleDefined) scale = 1; //Don't know what to set here
                if (!projectionDefined) projection = DefinedViewportProjection.None;
            } 
            else
            {
                //need a default view for view to start with - can't set attributes on a null object
                view = new ModelView(new Rhino.DocObjects.ViewportInfo(RhinoDoc.ActiveDoc.Views.GetStandardRhinoViews()[0].MainViewport));
                view.ToViewportInfo().TargetPoint = targetBBox.IsValid ? targetBBox.Center : view.ToViewportInfo().TargetPoint;
                switch (projection)
                {
                    case DefinedViewportProjection.Perspective:
                        {
                            view.ToViewportInfo().ChangeToPerspectiveProjection(10,true,50);
                            break;
                        }
                    case DefinedViewportProjection.TwoPointPerspective:
                        {
                            view.ToViewportInfo().ChangeToTwoPointPerspectiveProjection(10,Vector3d.ZAxis,50);
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

            if (Execute || run)
            {
                if (pageView != null)
                {
                    //goal: Check that detail fits on page
                    Rectangle3d pageBounds = new Rectangle3d(Plane.WorldXY, pageView.PageWidth, pageView.PageHeight);
                    bool upperLeft = pageBounds.Contains(dBounds.Corner(3)) == PointContainment.Inside;
                    bool lowerRight = pageBounds.Contains(dBounds.Corner(1)) == PointContainment.Inside;
                    if (!upperLeft || !lowerRight)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "New detail is (atleast partially) outside of layout boundaries");

                    var detail = pageView.AddDetailView("ModelView", new Point2d(dBounds.Corner(3)), new Point2d(dBounds.Corner(1)), projection);
                    pageView.SetPageAsActive();
                    doc.Views.ActiveView = pageView;
                    doc.Views.Redraw();
                    if (detail != null)
                    {
                        result = ReviseDetail(detail, targetBBox, scale, projection, displayMode, view.ToViewportInfo());
                    }

                    ModelView newView= new ModelView(new Rhino.DocObjects.ViewportInfo(detail.Viewport));
                    ModelDisplayMode newDisplayMode = new ModelDisplayMode(displayMode);
                    GH_DetailView newDetail = new GH_DetailView(detail.Id);

                    DA.SetData("Result", result);
                    //DA.SetData("Page", pageView.PageNumber);
                    DA.SetData("Detail", newDetail);
                    DA.SetData("Target", detail.Viewport.CameraTarget);
                    DA.SetData("Scale", detail.DetailGeometry.PageToModelRatio);
                    DA.SetData("Projection", detail.Viewport.Name);
                    DA.SetData("Display", newDisplayMode);
                    DA.SetData("View", newView);
                }

            }
           

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
            //List<string> pageViewNames = ValList.GetStandardViewList();
            string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
            List<string> projNames = pNames.Select(v => v.ToString()).ToList();
            List<int> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).Select(c => (int)c).ToList();
            List<string> projVals = pVals.ConvertAll(v => v.ToString());


            if (!ValList.AddOrUpdateValueList(this, 6, "Views", "Pick Projection: ", projNames, projVals))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + 0 + "] failed to update");

            ExpireSolution(true);
        }

        private void Menu_DisplayClick(object sender, EventArgs e)
        {
            List<string> dNames = ValList.GetDisplaySettingsList(false);
            List<string> dLocalNames = ValList.GetDisplaySettingsList(true);

            if (!ValList.AddOrUpdateValueList(this, 3, "Display", "Pick Display: ", dLocalNames, dNames))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + 0 + "] failed to update");

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

            Params.Input[1].ObjectChanged += InputParamChanged;
            Params.Input[3].ObjectChanged += InputParamChanged;
            Params.Input[6].ObjectChanged += InputParamChanged;

            _handled = true;
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            SetupEventHandlers();
        }


        public void InputParamChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (sender.NickName == Params.Input[1].NickName)
            {
                // optional feedback
                // Rhino.RhinoApp.WriteLine("This is the right input");

                var pageDictionary = Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews().ToDictionary(v => v.PageName, v => v.PageNumber);
                List<string> pageViewNames = pageDictionary.Keys.ToList();
                List<string> layoutIndices = new List<string>();
                for (int i = 0; i < pageViewNames.Count; i++)
                    layoutIndices.Add(pageDictionary[pageViewNames[i]].ToString());

                //try to modify input as a valuelist
                try
                {
                    ValList.UpdateValueList(this, 1, "Layouts", "Pick Layout(s): ", pageViewNames, layoutIndices);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
                catch (Exception) { };
            }

            if (sender.NickName == Params.Input[6].NickName)
            {
                string[] pNames = Enum.GetNames(typeof(DefinedViewportProjection));
                List<string> projNames = pNames.Select(v => v.ToString()).ToList();
                List<int> pVals = ((DefinedViewportProjection[])Enum.GetValues(typeof(DefinedViewportProjection))).Select(c => (int)c).ToList();
                List<string> projVals = pVals.ConvertAll(v => v.ToString());

                //try to modify input as a valuelist
                try
                {
                    ValList.UpdateValueList(this, 6, "Views", "Pick Projection: ", projNames, projVals);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
                catch (Exception) { };
            }

            if (sender.NickName == Params.Input[3].NickName)
            {
                List<string> displayNames = ValList.GetDisplaySettingsList(true);
                List<string> displayVals = ValList.GetDisplaySettingsList(false);

                //try to modify input as a valuelist
                try
                {
                    ValList.UpdateValueList(this, 3, "Display", "Pick Display: ", displayNames, displayVals);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
                catch (Exception) { };
            }
        }


        #endregion AutoValueList

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutNewDetail_bitmap;
         
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("75f7fa62-8641-11ee-b9d1-0242ac120002"); }
        }
    }
}