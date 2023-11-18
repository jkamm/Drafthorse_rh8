using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Drafthorse.Component.Layout
{
    public class DH_Component : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public DH_Component()
          : base("Deconstruct Layout", "Dec Layout",
            "Deconstruct a Layout",
            "Drafthorse", "Layout-Edit")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport(), "Layout", "L", "Input Layout", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Page Number", "#", "Number in the Layout table", GH_ParamAccess.item);
            pManager.AddTextParameter("Page Name", "N", "Page name", GH_ParamAccess.item);
            pManager.AddParameter(new Grasshopper.Rhinoceros.Display.Params.Param_ModelView(), "View", "V", "LayoutView", GH_ParamAccess.item);
            pManager.AddGenericParameter("Details", "D", "Details that appear on the page", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "Width of the Layout Page", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height of the Layout Page", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Rhinoceros.Display.ModelPageViewport page = new Grasshopper.Rhinoceros.Display.ModelPageViewport();
            DA.GetData("Layout", ref page);

            if (page != null)
            {
                RhinoPageView rhinoPage = Array.Find(RhinoDoc.ActiveDoc.Views.GetPageViews(),
                    (x) => x.MainViewport.Id.Equals(page.Id));

                List<GH_DetailView> gH_Details = rhinoPage.GetDetailViews().Select(v => new GH_DetailView(v.Id)).ToList();

                double pageWidth = rhinoPage.PageWidth;
                double pageHeight = rhinoPage.PageHeight;

                string pageName = page.Name;
                int? pageNumber = page.PageNumber;
                Grasshopper.Rhinoceros.Display.ModelView view = page.View;
                var path = page.Path;

                Grasshopper.Rhinoceros.ModelUserText userText = new Grasshopper.Rhinoceros.ModelUserText();
                List<string> keys = new List<string>();
                List<string> vals = new List<string>();

                if (!page.UserText.IsEmpty)
                {
                    userText = page.UserText;
                    keys.AddRange(userText.Keys);
                    vals.AddRange(userText.Values);
                }

                DA.SetData("Page Name", pageName);
                DA.SetData("Page Number", pageNumber);
                DA.SetData("View", view);
                DA.SetDataList("Details", gH_Details);
                DA.SetData("Width", pageWidth);
                DA.SetData("Height", pageHeight);

            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("938e980b-c4af-4484-b7e7-9b20687fda2d");
    }
}