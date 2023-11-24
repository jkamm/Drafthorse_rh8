using System;
using Grasshopper.Kernel;
using static Drafthorse.Helper.Layout;

namespace Drafthorse.Component.Layout.List2
{
    public class LayoutCopy :Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the Copy class.
        /// </summary>
        public LayoutCopy()
          : base("Copy Layout", "CopyLayout",
              "Instantiate a Layout from a template in the document",
              "Drafthorse", "Layout")
        {
            ButtonName = "Make";
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var bToggleParam = new Drafthorse.Params.Param_BooleanToggle();
            pManager.AddParameter(bToggleParam, "Run", "R", "Do not use button to activate - toggle only", GH_ParamAccess.item);
            var layoutParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            pManager.AddParameter(layoutParam, "Template", "T", "Template Layout", GH_ParamAccess.item);
            pManager.AddTextParameter("NewName", "N", "Name of Copy", GH_ParamAccess.item);
            
            Params.Input[0].Optional = true;
            Params.Input[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            var layoutParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            pManager.AddParameter(layoutParam, "Layout", "L", "Layouts Added to Document", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //define input variables
            bool run = false;
            DA.GetData("Run", ref run);
            //if (!DA.GetData("Run", ref run)) return;

            var templateRef = new Grasshopper.Rhinoceros.Display.ModelPageViewport();
            DA.GetData("Template", ref templateRef);

            //define local variables
            //int newIndex = new int();
            string newName = "";

            #region EscapeBehavior
            //Esc behavior code snippet from 
            // http://james-ramsden.com/you-should-be-implementing-esc-behaviour-in-your-grasshopper-development/
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            if (run || Execute)
            {
                Rhino.Display.RhinoPageView template = GetPage((int)templateRef.PageNumber);
                if (template == null) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Template page is null");

                Rhino.Display.RhinoPageView dup = template.Duplicate(true);

                //if no newName is defined, then set newName to template + PageNumber
                if (!DA.GetData("NewName", ref newName)) newName = template.PageName + "." + dup.PageNumber.ToString();

                //rename duplicate
                //dup.PageName = newName;
                //int newIndex = dup.PageNumber;
                RefreshView(dup);

                var newPage = new Grasshopper.Rhinoceros.Display.ModelPageViewport(dup.MainViewport);

                DA.SetData("Layout", newPage);
                
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.CopyLayout;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("35457848-2D5F-47AD-8118-8E7586430DB7"); }
        }
    }
}