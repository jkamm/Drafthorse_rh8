﻿using Grasshopper.Kernel;
using System;
//using System.Xml.Linq;

namespace Drafthorse.Component
{
    public class MakePageActive : Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MakePageActive()
          : base("Make Page Active", "Active Page",
              "Make a page active (primarily for Baking)",
              "Drafthorse", "Layout")
        {
            ButtonName = "Activate";
        }

        public override GH_Exposure Exposure => GH_Exposure.quarternary;
        int in_page;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var bToggleParam = new Params.Param_BooleanToggle();
            pManager.AddParameter(bToggleParam, "Run", "R", "run using an input", GH_ParamAccess.item);
            Params.Input[0].Optional = true;
            //pManager.AddIntegerParameter("Index", "Li", "Indices for Layouts", GH_ParamAccess.item);
            var pageParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            in_page = pManager.AddParameter(pageParam, "Page", "P", "Layout Page(s) to make active", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.Register_BooleanParam("Result", "R", "Result of Operation");
            pManager.Register_StringParam("Name", "N", "Active Layout Name");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            
            bool result = false;
            string name = null;

            Grasshopper.Rhinoceros.Display.ModelPageViewport page = new Grasshopper.Rhinoceros.Display.ModelPageViewport();
            DA.GetData(in_page, ref page);

            int? LayoutIndex = page.PageNumber;
            if (LayoutIndex == null) return;
            //DA.GetData("Index", ref LayoutIndex);

            bool run = false;
            DA.GetData("Run", ref run);

            #region EscapeBehavior
            //Esc behavior code snippet from
            // http://james-ramsden.com/you-should-be-implementing-esc-behaviour-in-your-grasshopper-development/

            if (GH_Document.IsEscapeKeyDown())
            {
                OnPingDocument().RequestAbortSolution();
                return;
            }
            #endregion EscapeBehavior

            if (run || Execute)
            {
                Rhino.Display.RhinoPageView activePage = Helper.Layout.GetPage((int)LayoutIndex);
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView = activePage;
                result = true;
                name = activePage.PageName;
            }
            DA.SetData("Result", result);
            DA.SetData("Name", name);
                     
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.MakePageActive;
        
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("dc6c6204-0925-4918-8c7b-5f9901ba8e6e"); }
        }
    }
}