using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using static Drafthorse.Helper.Layout;

namespace Drafthorse.Component.Detail
{
    public class DetailZoomBB : Base.DH_ButtonComponent
    {

        /// <summary>
        /// Initializes a new instance of the ReplaceDetails class.
        /// </summary>
        public DetailZoomBB()
          : base("Zoom Detail to BBox", "ZoomDetail",
              "Zoom a detail view to an object's bounding box",
              "Drafthorse", "Detail")
        {
            ButtonName = "Zoom";
            Hidden = true;
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Params.Param_BooleanToggle(), "Run", "R", "run using an input", GH_ParamAccess.item);
            pManager.AddParameter(new Param_Guid(), "GUID", "G", "GUID for Detail Object", GH_ParamAccess.item);
            pManager.AddBoxParameter("Bounding Box", "B", "Target Bounding Box for Detail", GH_ParamAccess.item);
                        
            Params.Input[0].Optional = true;
            Params.Input[2].Optional = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Success or Failure for each detail", GH_ParamAccess.item);
            pManager.AddParameter(new Param_Guid(), "GUID", "G", "GUID for Detail Object", GH_ParamAccess.item);
            pManager.AddPointParameter("Target", "T", "Camera Target for Detail", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale", "S", "Page Units per Model Unit", GH_ParamAccess.item);
            
        }

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

            bool run = false;
            DA.GetData("Run", ref run);

            //used for qualified override
            //bool targetDefined = false;


            Guid detailGUID = Guid.Empty;
            DA.GetData("GUID", ref detailGUID);
            Rhino.DocObjects.DetailViewObject detail = Rhino.RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as Rhino.DocObjects.DetailViewObject; ;
            if (detail == null)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is not valid");
                return;
            }

            Box targetBox = Box.Unset;
            DA.GetData("Bounding Box", ref targetBox);
            BoundingBox selfBBox = new BoundingBox(targetBox.X.Min, targetBox.Y.Min, targetBox.Z.Min, targetBox.X.Max, targetBox.Y.Max, targetBox.Z.Max);
            BoundingBox targetBBox = targetBox.BoundingBox;
            //BoundingBox targetBBox = selfBBox;


            if (Execute || run)
            {
                Rhino.Commands.Result detailResult = ReviseDetail(detail, targetBBox);
                
                DA.SetData("Result", detailResult);
            }

           
            DA.SetData("GUID", detailGUID);
            DA.SetData("Target", detail.Viewport.CameraTarget);
            DA.SetData("Scale", detail.DetailGeometry.PageToModelRatio);
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null; 
        //Drafthorse_rh8.Properties.Resources.LayoutDetail_bitmap;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("761F252C-D340-4E60-85B7-031DD5D6CE06"); }
        }
    }
}