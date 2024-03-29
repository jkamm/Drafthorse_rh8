﻿using Grasshopper.Kernel;
using System;

namespace Drafthorse.Component.UserText
{
    public class LayoutUserTextAccess_OBSOLETE : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AccessLayoutUserText class.
        /// </summary>
        public LayoutUserTextAccess_OBSOLETE()
          : base("Access Layout UserText", "GetLOValue",
              "Get Value for a Key on a Layout",
              "Drafthorse", "User Text")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Index", "Li", "Indices for Layouts. \nGet using 'Get Layout Index'", GH_ParamAccess.item);
            pManager.AddTextParameter("Keys", "K", "Key(s) to return Value for.", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Values", "V", "Values corresponding to Key", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int index = new int();
            DA.GetData("Index", ref index);

            string iKey = string.Empty;
            DA.GetData("Keys", ref iKey);

            Rhino.Display.RhinoPageView pageView = Helper.Layout.GetPage(index);

            if (pageView != null)
            {
                var userText = pageView.MainViewport.GetUserStrings();

                string userVal = userText.Get(iKey);

                DA.SetData("Values", userVal);
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.AccessLayoutVal;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0224137f-3de8-411c-815a-69554f5e8b15"); }
        }
    }
}