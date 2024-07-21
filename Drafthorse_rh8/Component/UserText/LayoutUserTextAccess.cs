using Grasshopper.Kernel;
using System;

namespace Drafthorse.Component.UserText
{
    public class LayoutUserTextAccess : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AccessLayoutUserText class.
        /// </summary>
        public LayoutUserTextAccess()
          : base("Access Layout UserText", "GetLOValue",
              "Get Value for a Key on a Layout",
              "Drafthorse", "User Text")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        int in_page;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var pageParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            in_page = pManager.AddParameter(pageParam, "Page", "P", "Layout Page(s) to modify", GH_ParamAccess.item); 
            //pManager.AddIntegerParameter("Index", "Li", "Indices for Layouts. \nGet using 'Get Layout Index'", GH_ParamAccess.item);
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
            Grasshopper.Rhinoceros.Display.ModelPageViewport page = new Grasshopper.Rhinoceros.Display.ModelPageViewport();
            DA.GetData(in_page, ref page);
            if (page.PageNumber == null) return;

            int index = (int)page.PageNumber;
            //DA.GetData("Index", ref index);

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
            get { return new Guid("4656efb1-7689-463b-bdab-98deb8d98135"); }
        }
    }
}