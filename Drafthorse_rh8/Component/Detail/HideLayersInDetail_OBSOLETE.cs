using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using Rhino.DocObjects;
using Grasshopper.Kernel.Types;

namespace Drafthorse.Component.Detail
{
    public class HideLayersInDetail_OBSOLETE : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HideLayerInDetail class.
        /// </summary>
        public HideLayersInDetail_OBSOLETE()
          : base("HideLayerInDetail", "HideLayerDetail",
              "Hide the target layer in the target Detail (WIP)",
              "Drafthorse", "Detail")
        {
        }
        private GH_Exposure Exposure = GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView to modify", GH_ParamAccess.item);
            //pManager.AddParameter(new Params.Param_BooleanToggle(), "Run", "R", "Do not use button to activate - toggle only", GH_ParamAccess.item);
            //pManager.AddParameter(new Param_Guid(), "GUID", "G", "GUID for Detail Object", GH_ParamAccess.item);
            var layerParam = new Grasshopper.Rhinoceros.Model.Params.Param_ModelLayer();
            pManager.AddParameter(layerParam, "Layers", "L", "Layers to set visibility in Detail", GH_ParamAccess.list);
            pManager.AddBooleanParameter("States", "S", "Visibility State: True = Visible, False = Hidden", GH_ParamAccess.list);

            Params.Input[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            var viewParam = new Param_DetailView();
            pManager.AddParameter(viewParam, "Detail View", "D", "DetailView that was modified", GH_ParamAccess.item);
            //pManager.AddParameter(new Param_Guid(), "GUID", "G", "GUID for Detail Object", GH_ParamAccess.item);
            var layerParam = new Grasshopper.Rhinoceros.Model.Params.Param_ModelLayer();
            pManager.AddParameter(layerParam, "Layers", "L", "Layers to set visibility in Detail", GH_ParamAccess.list);
            pManager.AddBooleanParameter("States", "S", "Visibility State: True = Visible, False = Hidden", GH_ParamAccess.list);
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

            List<Grasshopper.Rhinoceros.Model.ModelLayer> layerList = new List<Grasshopper.Rhinoceros.Model.ModelLayer>();
            List<Grasshopper.Rhinoceros.Model.ModelLayer> newLayerList = new List<Grasshopper.Rhinoceros.Model.ModelLayer>();
            DA.GetDataList("Layers", layerList);

            List<bool> stateList = new List<bool>();
            List<bool> newStateList = new List<bool>();

            DA.GetDataList("States", stateList);

            RhinoDoc doc = RhinoDoc.ActiveDoc;

            List<string> results = new List<string>();

            for (int i = 0; i < layerList.Count; i++)
            {
                var ghLayer = layerList[i];
                var state = stateList[Math.Min(i, stateList.Count - 1)];
                
                Guid? layerGuid = ghLayer.Id;

                if (layerGuid.HasValue)
                {
                    // Get the active Rhino document


                    // Try to find the layer by name
                    Layer rhLayer = doc.Layers.FindId(layerGuid.Value);
                    rhLayer.SetPerViewportVisible(detailGUID, state);
                    string result = state ? "visible" : "hidden";
                    string message = "Success! Layer " + ghLayer.Path.ToString() + " set to " + result;
                    results.Add(message);
                    newStateList.Add(state);
                    newLayerList.Add(ghLayer);
                }
                else results.Add("failure :(");

            }
            DA.SetData("Detail View", detailView);
            //DA.SetData("GUID", detailGUID);
            DA.SetDataList("Layers", newLayerList);
            DA.SetDataList("States", newStateList);            
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
            get { return new Guid("4506CA3D-E2F9-446A-A488-F60968C8B57E"); }
        }
    }
}