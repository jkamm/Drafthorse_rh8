using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Drafthorse.Component.Layout
{
    public class Copy : Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the Copy class.
        /// </summary>
        public Copy()
          : base("Copy Layout", "CopyLayout",
              "Instantiate a Layout from a template in the document",
              "DraftHorse", "Layout-Add")
        {
            ButtonName = "Copy";
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var bToggleParam = new DraftHorse.Params.Param_BooleanToggle();
            pManager.AddParameter(bToggleParam, "Run", "R", "Do not use button to activate - toggle only", GH_ParamAccess.item);
            Params.Input[0].Optional = true;
            pManager.AddIntegerParameter("Template Index", "Li[]", "Index of Template Layout \nAdd ValueList to get list of layouts", GH_ParamAccess.item);
            pManager.AddTextParameter("NewName", "N", "Name of Copy", GH_ParamAccess.item);
            Params.Input[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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
            get { return new Guid("35457848-2D5F-47AD-8118-8E7586430DB7"); }
        }
    }
}