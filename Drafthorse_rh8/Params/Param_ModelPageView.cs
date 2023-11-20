using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drafthorse.Params
{
    internal class Param_LayoutPage : Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
    {
        public override Guid ComponentGuid => new Guid("{44bcca80-32ae-4e2f-ad6d-c6fd316eed30}");

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public Param_LayoutPage()
            : base() 
        {
            Category = "Drafthorse";
            SubCategory = "Layout";
        }
    }
}
