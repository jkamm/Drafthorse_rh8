﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Display;
using Rhino.Geometry;
using System;

namespace Drafthorse.Component.Detail
{
    public class DetailDeconstruct : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DeconstructDetail class.
        /// </summary>
        public DetailDeconstruct()
          : base("Deconstruct Detail", "DecDetail",
              "Deconstruct a Detail into its Composition and Attributes",
              "Drafthorse", "Detail")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            //var guidParam = new Grasshopper.Kernel.Parameters.Param_Guid();
            //pManager.AddParameter(guidParam, "Detail GUID", "D", "GUID for Detail Object", GH_ParamAccess.item);
            pManager.AddParameter(new Param_DetailView(), "Detail", "D", "Detail Object", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            //pManager.AddGeometryParameter("Geometry", "G", "Underlying Geometry for Detail View", GH_ParamAccess.item);
            //pManager.AddTextParameter("View", "V", "Name of Viewport", GH_ParamAccess.item);
            //pManager.AddParameter(new Param_DetailView(), "Detail", "D", "Detail Object", GH_ParamAccess.item);
            var viewParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelView();
            viewParam.Hidden = true;
            pManager.AddParameter(viewParam, "View", "V", "Detail View", GH_ParamAccess.item);
            //pManager.AddTextParameter("ViewName", "Vn", "Detail View Name", GH_ParamAccess.item);
            var displayParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelDisplayMode();
            pManager.AddParameter(displayParam, "Display", "D", "Model Display Mode", GH_ParamAccess.item);
            //pManager.AddTextParameter("Display", "D", "Display Mode", GH_ParamAccess.item);
            pManager.AddTransformParameter("ToWorld", "tW", "Page to World Transform", GH_ParamAccess.item);
            pManager.AddTransformParameter("ToPage", "tP", "World to Page Transform", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale", "S", "Page units/model units quotient.", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Bounds", "B", "Boundary of detail viewport in Rhino Space", GH_ParamAccess.item);
            pManager.AddPointParameter("Target", "T", "Viewport Camera Target", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Attributes", "A", "Detail Attributes", GH_ParamAccess.item);
            //pManager.AddTextParameter("Name", "N", "Detail Name", GH_ParamAccess.item);
           
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Guid detailGUID = Guid.Empty;
            //DA.GetData("Detail GUID", ref detailGUID);

            GH_DetailView gH_DetailView = null;
            DA.GetData("Detail", ref gH_DetailView);
            detailGUID = gH_DetailView.ReferenceID;

            Rhino.DocObjects.DetailViewObject detail = Rhino.RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as Rhino.DocObjects.DetailViewObject; ;
            if (detail == null) return;

            //Curve detailGeo = detail.Geometry as Curve;

            Rhino.Display.RhinoViewport viewport = detail.Viewport;
            string viewName = viewport.Name;
            ModelView view = new ModelView(viewport);

            System.Drawing.Rectangle bounds = viewport.Bounds;
            Rectangle3d rhinoBounds = new Rectangle3d(Plane.WorldXY, new Point3d(bounds.Left, bounds.Bottom, 0),
                new Point3d(bounds.Right, bounds.Top, 0));
            var targetPt = viewport.CameraTarget;

            //Rhino.Display.DisplayModeDescription viewDisplay = viewport.DisplayMode;

            Rhino.DocObjects.ObjectAttributes att = detail.Attributes;

            //Guid viewportId = att.ViewportId;
            //var parentView = viewport.ParentView;

            //var parent = parentView
            //get viewport name from ViewportId
            //return viewportName which should be the layout it belongs to.

            Transform pageToWorld = detail.PageToWorldTransform;
            Transform worldToPage = detail.WorldToPageTransform;

            DetailView detailView = detail.DetailGeometry;
            double scale = detailView.PageToModelRatio;
            var displayMode = new Grasshopper.Rhinoceros.Display.ModelDisplayMode(viewport.DisplayMode);
            //string viewName = detail.Name;
            //detailView.            

            //DA.SetData("Geometry", detailGeo);
            //DA.SetData("Detail", gH_DetailView ?? null);
            DA.SetData("View", view);
            //DA.SetData("ViewName", viewName);
            DA.SetData("Display", displayMode);
            DA.SetData("ToWorld", pageToWorld);
            DA.SetData("ToPage", worldToPage);
            DA.SetData("Scale", scale);
            DA.SetData("Bounds", rhinoBounds);
            DA.SetData("Target", targetPt);
            //DA.SetData("Attributes", att);
            //DA.SetData("Name", detail.Name);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.Dec_Detail;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("338c646a-8641-11ee-b9d1-0242ac120002"); }
            
        }
    }
}