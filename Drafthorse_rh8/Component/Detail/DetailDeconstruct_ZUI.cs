using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Drafthorse.Component.Base;
using Grasshopper.Rhinoceros.Display.Params;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino;
using Grasshopper.Rhinoceros.Display;
using Rhino.DocObjects;

namespace Layout
{
    public class DetailDeconstruct_ZUI : ZuiComponent
    {
        private static readonly ParamDefinition[] inputs = new ParamDefinition[1]
        {
            new ParamDefinition (new Param_DetailView
            {
                Name = "Detail",
                NickName = "Dt",
                Description = "Detail Object",
                //Optional = false,
                Access = GH_ParamAccess.item
            }, ParamRelevance.Binding),
        };

        private static readonly ParamDefinition[] outputs = new ParamDefinition[7]
        {
            new ParamDefinition(new Param_ModelView
            {
                Name = "View",
                NickName = "V",
                Description = "Detail View",
                Hidden = true,
            }, ParamRelevance.Primary),
            new ParamDefinition (new Param_ModelDisplayMode
            {
                Name = "Display",
                NickName = "D",
                Description = "Model Display Mode",
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Transform
            {
                Name = "ToWorld",
                NickName = "Tw",
                Description = "Page to World Transform",
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Transform
            {
                Name = "ToPage",
                NickName = "Tp",
                Description = "World to Page Transform",
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Number
            {
                Name = "Scale",
                NickName = "S",
                Description = "Page Units per Model Unit",
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Rectangle
            {
                Name = "Bounds",
                NickName = "B",
                Description = "Boundary of detail viewport in Rhino Space"
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Point
            {
                Name = "Target",
                NickName = "T",
                Description = "Camera Target",
                Optional = true,
            }, ParamRelevance.Primary),
        };
        protected override ParamDefinition[] Inputs => inputs;
        protected override ParamDefinition[] Outputs => outputs;


        /// <summary>
        /// Initializes a new instance of the DetailDeconstruct_ZUI class.
        /// </summary>
        public DetailDeconstruct_ZUI()
          : base("DetailDeconstruct_ZUI", "Detail Dec",
              "Deconstruct a detail to access its attributes",
              "Drafthorse", "Detail")
        {
            Hidden = true;
        }
        //public override GH_Exposure Exposure => GH_Exposure.secondary;
        public override GH_Exposure Exposure => GH_Exposure.hidden;


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int num = 0;

            Guid detailGUID = Guid.Empty;
            if (!TryGetData<GH_DetailView>(DA, inputs[num++].Param.Name, out GH_DetailView value1)) return;

            if (value1.IsValid) detailGUID = value1.ReferenceID;

            Rhino.DocObjects.DetailViewObject detail = RhinoDoc.ActiveDoc.Objects.FindId(detailGUID) as Rhino.DocObjects.DetailViewObject; ;
            if (detail == null) return;

            //Get Attributes 

            Rhino.Display.RhinoViewport viewport = detail.Viewport;
            string viewName = viewport.Name;
            ModelView view = new ModelView(viewport);

            System.Drawing.Rectangle bounds = viewport.Bounds;
            Rectangle3d rhinoBounds = new Rectangle3d(Plane.WorldXY, new Point3d(bounds.Left, bounds.Bottom, 0),
                new Point3d(bounds.Right, bounds.Top, 0));
            var targetPt = viewport.CameraTarget;
                        
            Rhino.DocObjects.ObjectAttributes att = detail.Attributes;
                        
            Transform pageToWorld = detail.PageToWorldTransform;
            Transform worldToPage = detail.WorldToPageTransform;

            DetailView detailView = detail.DetailGeometry;
            double scale = detailView.PageToModelRatio;
            var displayMode = new ModelDisplayMode(viewport.DisplayMode);

            // set attributes to outputs
            int num2 = 0;

            TrySetData(DA, outputs[num2++].Param.Name, () => view);

            TrySetData(DA, outputs[num2++].Param.Name, () => displayMode);

            TrySetData(DA, outputs[num2++].Param.Name, () => pageToWorld);

            TrySetData(DA, outputs[num2++].Param.Name, () => worldToPage);

            TrySetData(DA, outputs[num2++].Param.Name, () => scale);

            TrySetData(DA, outputs[num2++].Param.Name, () => rhinoBounds);

            TrySetData(DA, outputs[num2++].Param.Name, () => targetPt);
        
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
            get { return new Guid("05BD5CDD-3744-49F9-85F9-35F1E69FCFC8"); }
        }
    }
}