using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Model;
using Grasshopper.Rhinoceros.Model.Params;
using Rhino.DocObjects;
using Rhino;
using Rhino.Geometry;

namespace Drafthorse_rh8.Component.Detail
{
    public class HideObjInDetail : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HideObjInDetail class.
        /// </summary>
        public HideObjInDetail()
          : base("Hide Object In Detail", "HideObjInDet",
              "Hide a single object in a single detail",
              "Drafthorse", "Detail")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView to modify", GH_ParamAccess.item);
            var modelObjectParam = new Param_ModelObject();
            pManager.AddParameter(modelObjectParam, "Model Object", "O", "Model Objects to modify", GH_ParamAccess.item);
            pManager.AddBooleanParameter("State", "S", "Visibility state: Show = True, Hide = False", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView to modify", GH_ParamAccess.item);
            var modelObjectParam = new Param_ModelObject();
            pManager.AddParameter(modelObjectParam, "Model Object", "O", "Model Objects to modify", GH_ParamAccess.list);
            pManager.AddBooleanParameter("State", "S", "Visibility state: Show = True, Hide = False", GH_ParamAccess.list);
            pManager.AddTextParameter("Result", "R", "Object show/hide state after", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Guid detailGUID = Guid.Empty;

            GH_DetailView detailView = null;
            DA.GetData("Detail View", ref detailView);
            detailGUID = detailView.ReferenceID;

            DetailViewObject detail = RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as DetailViewObject;
            if (detail == null)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is not valid");
                return;
            }

            ModelObject thisObject = new ModelObject();
            ModelObject newObject = new ModelObject();
            DA.GetData("Model Object", ref thisObject);

            bool thisState = true;
            bool newState = true;

            DA.GetData("State", ref thisState);

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            string result = String.Empty;

            bool thisResult = false;

           
                ModelObject obj = thisObject;
                //bool state = stateList[Math.Min(i, stateList.Count - 1)];

            if (obj.Id.HasValue)
            {
                RhinoObject rhObj = doc.Objects.FindId(obj.Id.Value);
                ObjectAttributes rhAtt = rhObj.Attributes;
                if (!thisState) rhAtt.AddHideInDetailOverride(detailGUID);
                else rhAtt.RemoveHideInDetailOverride(detailGUID);
                thisResult = doc.Objects.ModifyAttributes(rhObj, rhAtt, true);
                newObject = obj;
                newState = thisState;
                string visibility = thisState ? "show" : "hide";
                string message = thisResult ? obj.ToString() + "set to " + visibility : obj.ToString() + " - failure :(";
                result = message;
            }
            

            DA.SetData("Detail View", detailView);
            DA.SetData("Model Object", newObject);
            DA.SetData("State", newState);
            DA.SetData("Results", result);
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
            get { return new Guid("2671F865-5AA2-4AD6-B131-19934B682EB2"); }
        }
    }
}