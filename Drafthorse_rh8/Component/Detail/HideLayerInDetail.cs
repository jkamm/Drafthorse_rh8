using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Rhinoceros.Model;

namespace Drafthorse_rh8.Component.Detail
{
    public class HideLayerInDetail : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HideLayerInDetail class.
        /// </summary>
        public HideLayerInDetail()
          : base("HideLayerInDetail", "HideLayerInDet",
              "Hide or Show a Layer in a Detail",
              "Drafthorse", "Detail")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail", "D", "DetailView to modify", GH_ParamAccess.item);
            var layerParam = new Grasshopper.Rhinoceros.Model.Params.Param_ModelLayer();
            pManager.AddParameter(layerParam, "Layer", "L", "Layer to set visibility in Detail", GH_ParamAccess.item);
            pManager.AddBooleanParameter("State", "S", "Visibility State: True = Visible, False = Hidden", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail", "D", "DetailView that was modified", GH_ParamAccess.item);
            var layerParam = new Grasshopper.Rhinoceros.Model.Params.Param_ModelLayer();
            pManager.AddParameter(layerParam, "Layer", "L", "Layers to set visibility in Detail", GH_ParamAccess.item);
            pManager.AddBooleanParameter("State", "S", "Visibility State: True = Visible, False = Hidden", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Guid detailGUID = Guid.Empty;
            //DA.GetData("GUID", ref detailGUID);

            GH_DetailView detailView = null;
            DA.GetData("Detail View", ref detailView);
            detailGUID = detailView.ReferenceID;

            DetailViewObject detail = RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as DetailViewObject;
            if (detail == null)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is not valid");
                return;
            }

            ModelLayer thisLayer = new ModelLayer();
            ModelLayer newLayer = new ModelLayer();
            DA.GetData("Layer", ref thisLayer);

            bool thisState = new bool();
            bool newState = new bool();

            DA.GetData("State", ref thisState);

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            string result = String.Empty;


            var ghLayer = thisLayer;
            var state = thisState;

            Guid? layerGuid = ghLayer.Id;

            if (layerGuid.HasValue)
            {
                Layer rhLayer = doc.Layers.FindId(layerGuid.Value);
                rhLayer.SetPerViewportVisible(detailGUID, state);
                string thisResult = state ? "visible" : "hidden";
                string message = "Success! Layer " + ghLayer.Path.ToString() + " set to " + thisResult;
                result = message;
                newState = state;
                newLayer = ghLayer;
            }
            else result = "failure :(";

            DA.SetData("Detail", detailView);
            DA.SetData("Layer", newLayer);
            DA.SetData("State", newState);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("E1FB329F-F3E2-4D08-BEF8-7928D5BCE4A7"); }
        }
    }
}