using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Model.Params;
using Rhino.DocObjects;
using Rhino;
using Grasshopper.Rhinoceros.Model;

namespace Drafthorse_rh8.Component.Detail
{
    public class HideModelObjectsInDetail_OBSOLETE : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HideModelObjectsInDetail class.
        /// </summary>
        public HideModelObjectsInDetail_OBSOLETE()
          : base("Hide Model Objects In Detail", "HideObjsInDet",
              "Hide or Show an object in a detail",
              "Drafthorse", "Detail")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView to modify", GH_ParamAccess.item);
            var modelObjectParam = new Param_ModelObject();
            pManager.AddParameter(modelObjectParam, "Model Objects", "O", "Model Objects to modify", GH_ParamAccess.list);
            pManager.AddBooleanParameter("States", "S", "Visibility state: Show = True, Hide = False", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView to modify", GH_ParamAccess.item);
            var modelObjectParam = new Param_ModelObject();
            pManager.AddParameter(modelObjectParam, "Model Objects", "O", "Model Objects to modify", GH_ParamAccess.list);
            pManager.AddBooleanParameter("States", "S", "Visibility state: Show = True, Hide = False", GH_ParamAccess.list);
            pManager.AddTextParameter("Results", "R", "Objects show/hide state after", GH_ParamAccess.list);
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

            List<Grasshopper.Rhinoceros.Model.ModelObject> objectList = new List<Grasshopper.Rhinoceros.Model.ModelObject>();
            List<Grasshopper.Rhinoceros.Model.ModelObject> newObjectList = new List<Grasshopper.Rhinoceros.Model.ModelObject>();
            DA.GetDataList("Model Objects", objectList);

            List<bool> stateList = new List<bool>();
            List<bool> newStateList = new List<bool>();

            DA.GetDataList("States", stateList);

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            List<string> results = new List<string>();

            bool result = false;

            for (int i = 0; i < objectList.Count; i++)
            {
                ModelObject obj = objectList[i];
                bool state = stateList[Math.Min(i, stateList.Count - 1)];

                if (obj.Id.HasValue)
                {
                    RhinoObject rhObj = doc.Objects.FindId(obj.Id.Value);
                    ObjectAttributes rhAtt = rhObj.Attributes;
                    if (!state) rhAtt.AddHideInDetailOverride(detailGUID);
                    else rhAtt.RemoveHideInDetailOverride(detailGUID);
                    result = doc.Objects.ModifyAttributes(rhObj, rhAtt,true);
                    newObjectList.Add(obj);
                    newStateList.Add(state);
                    string visibility = state ? "show" : "hide";
                    string message = result ? obj.ToString() + "set to " + visibility : obj.ToString() + " - failure :(";
                    results.Add(message);
                }
            }

            DA.SetData("Detail View", detailView);
            DA.SetDataList("Model Objects", newObjectList);
            DA.SetDataList("States", newStateList);
            DA.SetDataList("Results", results);

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
            get { return new Guid("D70B1317-3A82-45FD-845E-DCC90796C208"); }
        }
    }
}