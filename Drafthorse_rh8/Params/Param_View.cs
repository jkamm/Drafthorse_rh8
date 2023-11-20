using Grasshopper.Kernel;
using System;

namespace Drafthorse.Params
{
    public class Param_View : Grasshopper.Rhinoceros.Display.Params.Param_ModelView
    { 
        public Param_View()
            : base() 
        {
            Category = "Drafthorse";
            SubCategory = "Detail";
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        public override Guid ComponentGuid => new Guid("{3c6b92a6-06c1-4ac9-b91a-a01b183c411f}");
    }
}
