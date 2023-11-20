using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Rhinoceros.Display;

namespace Drafthorse.Component.Layout.List3
{
    public class LayoutDeconstruct : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public LayoutDeconstruct()
          : base("Deconstruct Layout Page", "Dec Layout",
            "Deconstruct a Layout Page to get Contents and Attributes",
            "Drafthorse", "Layout")
        {
        }
        int in_page;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var pageParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            in_page = pManager.AddParameter(pageParam, "Layout", "P", "Input Layout Page", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Page Number", "#", "Order of Page in Rhino", GH_ParamAccess.item);
            pManager.AddTextParameter("Page Name", "N", "Page name", GH_ParamAccess.item);
            //pManager.AddParameter(new Grasshopper.Rhinoceros.Display.Params.Param_ModelView(), "View", "V", " Detail View", GH_ParamAccess.item);
            
            pManager.AddGenericParameter("Details", "D", "Details that appear on the page", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "Width of the Layout Page", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height of the Layout Page", GH_ParamAccess.item);
            pManager.AddTextParameter("Units", "U", "Paperspace Units", GH_ParamAccess.item);
            /*
             */

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ModelPageViewport page = new ModelPageViewport();
            DA.GetData(in_page, ref page);

            if (page.PageNumber == null) return;
            RhinoPageView rhinoPage = Helper.Layout.GetPage((int)page.PageNumber);

            if (rhinoPage != null)
            {;

                List<GH_DetailView> gH_Details = rhinoPage.GetDetailViews().Select(v => new GH_DetailView(v.Id)).ToList();

                double pageWidth = rhinoPage.PageWidth;
                double pageHeight = rhinoPage.PageHeight;

                string pageName = page.Name;
                int? pageNumber = page.PageNumber;
                ModelView view = page.View;
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
                //DA.SetData("View", view);
                DA.SetDataList("Details", gH_Details);
                DA.SetData("Width", pageWidth);
                DA.SetData("Height", pageHeight);
                DA.SetData("Units", RhinoDoc.ActiveDoc.PageUnitSystem);

                

            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.Dec_Layout;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("938e980b-c4af-4484-b7e7-9b20687fda2d");
    }
}