using System;
using Grasshopper.Kernel;

namespace Drafthorse.Component.Base
{
    public abstract class DH_ButtonComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DH_ButtonComponent class.
        /// </summary>
        public DH_ButtonComponent(string name, string nickname, string description, string category, string subcategory)
          : base(name, nickname,
              description,
              category, subcategory)
        {
            Execute = false;
            ButtonName = "Execute";
        }

        public bool Execute { get; set; }
        public string ButtonName { get; set; }
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
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

        public virtual void OnButtonActivate(object sender, EventArgs e)
        {
            Execute = true;
            ExpireSolution(true);
        }

        protected override void AfterSolveInstance()
        {
            if (this.m_attributes is DH_ButtonComponentAttributes buttonComponent)
            {
                buttonComponent.Active = false;
                Execute = false;
            }
            base.AfterSolveInstance();
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
            get { return new Guid("F4578F1C-0D48-4AA5-A944-3DB97760F16A"); }
        }
    }
}