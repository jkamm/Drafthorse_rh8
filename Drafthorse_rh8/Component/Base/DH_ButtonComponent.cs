using System;
using Grasshopper.Kernel;

namespace Drafthorse.Component.Base
{
    public abstract class DH_ButtonComponent : GH_Component
    {
        public DH_ButtonComponent(string name, string nickname, string description, string category, string subcategory)
          : base(name, nickname, description, category, subcategory)
        {
            Execute = false;
            ButtonName = "Execute";
        }

        public bool Execute { get; set; }
        public string ButtonName { get; set; }
        //public override GH_Exposure Exposure => GH_Exposure.hidden;
        
        protected abstract override void RegisterInputParams(GH_Component.GH_InputParamManager pManager);
        
        protected abstract override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager);

        protected abstract override void SolveInstance(IGH_DataAccess DA);

        public virtual void OnButtonActivate(object sender, EventArgs e)
        {
            Execute = true;
            ExpireSolution(true);
        }

        protected override void AfterSolveInstance()
        {
            if (m_attributes is DH_ButtonComponentAttributes buttonComponent)
            {
                buttonComponent.Active = false;
                buttonComponent.Complete = true;
                Execute = false;
            }
            base.AfterSolveInstance();
        }
        public override void CreateAttributes()
        {
            m_attributes = new DH_ButtonComponentAttributes(this);
        }

        protected override void BeforeSolveInstance()
        {
            if (m_attributes is DH_ButtonComponentAttributes buttonComponent) buttonComponent.Complete = false;
            base.BeforeSolveInstance();
        }
    }
}