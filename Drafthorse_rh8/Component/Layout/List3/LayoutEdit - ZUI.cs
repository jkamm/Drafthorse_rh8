/*
using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System.Linq;
using static Drafthorse.Helper.Layout;
using IOComponents;

namespace Drafthorse.Component.Layout.List3
{
    public class LayoutEdit_ZUI : ZuiComponent
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        private static readonly ParamDefinition[] inputs = new ParamDefinition[7]
        {
            new ParamDefinition(new Params.Param_BooleanToggle
            {
                Name = "Run",
                NickName = "R",
                Description = "Do not use button to activate - toggle only",
                Optional = true,
            }, ParamRelevance.Secondary),
            new ParamDefinition(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
            {
                Name = "Layout Page",
                NickName = "P",
                Description = "Input Layout Page",
                Optional = false,
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_String
            {
                Name = "Name",
                NickName = "N",
                Description = "New Page Name",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Number
            {
                Name = "Width",
                NickName = "W",
                Description = "PageWidth",
                Optional = true,
            }, ParamRelevance.Primary),
             new ParamDefinition(new Param_Number
            {
                Name = "Height",
                NickName = "H",
                Description = "PageHeight",
                Optional = true,
            }, ParamRelevance.Primary),
              new ParamDefinition(new Param_String
            {
                Name = "Keys",
                NickName = "K",
                Description = "Keys for User Texts (unique)",
                Optional = true,
                Access = GH_ParamAccess.list,
            }, ParamRelevance.Secondary),
               new ParamDefinition(new Param_String
            {
                Name = "Values",
                NickName = "V",
                Description = "Values for User Texts (must match Key count)",
                Optional = true,
                Access = GH_ParamAccess.list,
            }, ParamRelevance.Secondary)
        };
        private static readonly ParamDefinition[] outputs = new ParamDefinition[7]
        {
             new ParamDefinition(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
            {
                Name = "Layout Page",
                NickName = "P",
                Description = "Input Layout Page",
                Optional = false,
            }, ParamRelevance.Binding),
             new ParamDefinition(new Param_Integer
            {
                Name = "Index",
                NickName = "i",
                Description = "Index of new Page Layout",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_String
            {
                Name = "Name",
                NickName = "N",
                Description = "New Page Name",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Param_Number
            {
                Name = "Width",
                NickName = "W",
                Description = "PageWidth",
                Optional = true,
            }, ParamRelevance.Primary),
             new ParamDefinition(new Param_Number
            {
                Name = "Height",
                NickName = "H",
                Description = "PageHeight",
                Optional = true,
            }, ParamRelevance.Primary),
              new ParamDefinition(new Param_String
            {
                Name = "Keys",
                NickName = "K",
                Description = "Keys for User Texts (unique)",
                Optional = true,
            }, ParamRelevance.Secondary),
               new ParamDefinition(new Param_String
            {
                Name = "Values",
                NickName = "V",
                Description = "Values for User Texts (must match Key count)",
                Optional = true,
            }, ParamRelevance.Secondary)
        };

        protected override ParamDefinition[] Inputs => inputs;
        protected override ParamDefinition[] Outputs => outputs;

        /// <summary>
        /// Initializes a new instance of the LayoutEdit_ZUI class.
        /// </summary>
        public LayoutEdit_ZUI()
          : base("Edit Layout", "LayoutEdit", "Modify Layout Attributes", "Drafthorse", "Layout")
        {
            
        }

        public override void VariableParameterMaintenance()
        {
            base.VariableParameterMaintenance();
        }
        
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int num = 0;
            bool run = false;
            if (!TryGetData<bool>(DA, inputs[num++].Param.Name, out var value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            if (!TryGetData<Grasshopper.Rhinoceros.Display.ModelPageViewport>(DA, inputs[num++].Param.Name, out var page)) return;

            if (page.PageNumber == null)
            {
                Rhino.RhinoApp.CommandLineOut.WriteLine("Page was null"); //feedback for debugging
                return;
            }

            Rhino.Display.RhinoPageView target = GetPage((int)page.PageNumber);

            if (target != null)
            {
                bool pressed = (base.Attributes as CustomAttributes).Pressed;

                int? newPageNumber = target.PageNumber;

                string newPageName = string.Empty;
                if (!TryGetData<string>(DA, inputs[num++].Param.Name, out newPageName)) newPageName = target.PageName;

                double newPageWidth = target.PageWidth;
                if (!TryGetData<double>(DA, inputs[num++].Param.Name, out double? value1)) newPageWidth = target.PageWidth;
                if (value1.HasValue) newPageWidth = value1.Value;

                double newPageHeight = target.PageHeight;
                if (!TryGetData<double>(DA, inputs[num++].Param.Name, out double? value2)) newPageHeight = target.PageHeight;
                if (value2.HasValue) newPageHeight = value2.Value;

                List<string> iKeys = new List<string>();
                TryGetDataList<string>(DA, inputs[num++].Param.Name, out string[] values3);
                if (values3 != null) iKeys = values3.ToList();

               
                List<string> iVals = new List<string>();
                TryGetDataList<string>(DA, inputs[num++].Param.Name, out string[] values4);
                if (values4 != null) iVals = values4.ToList();


                if (iVals.Count != iKeys.Count) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Key/Value pairs do not match. Check the data structure.");

                if (run || pressed)
                {

                    var uText = target.MainViewport.GetUserStrings();

                    IEnumerable<string> keysFound = from key in iKeys
                                                    where uText.Get(key) != null
                                                    select key;
                    if (keysFound.Count() != 0) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Duplicate Keys Found, all duplicates will be replaced");

                    for (int i = 0; i < iKeys.Count; i++)
                    {
                        target.MainViewport.SetUserString(iKeys[i], iVals[i]);
                    }

                    target.PageName = newPageName;
                    target.PageWidth = newPageWidth;
                    target.PageHeight = newPageHeight;
                    //target.PageNumber = (int)newPageNumber;
                }

                int pageNumber = target.PageNumber;
                string name = target.PageName;
                double pageWidth = target.PageWidth;
                double pageHeight = target.PageHeight;

                //Get UserText from MainViewPort of Layout
                var userText = target.MainViewport.GetUserStrings();
                string[] userVals = new string[userText.Count];
                if (userText.Count != 0)
                {
                    for (int i = 0; i < userText.Count; i++)
                    {
                        userVals[i] = userText.Get(i);
                    }
                }

                int num2 = 0;

                TrySetData(DA, outputs[num2++].Param.Name, () => page);
                TrySetData(DA, outputs[num2++].Param.Name, () => pageNumber);
                TrySetData(DA, outputs[num2++].Param.Name, () => name);
                TrySetData(DA, outputs[num2++].Param.Name, () => pageWidth);
                TrySetData(DA, outputs[num2++].Param.Name, () => pageHeight);
                TrySetDataList(DA, outputs[num2++].Param.Name, () => userText.AllKeys);
                TrySetDataList(DA, outputs[num2++].Param.Name, () => userVals);
            }
        }
        
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.EditLayout_bitmap;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3404781d-02a4-4d3a-a3e9-9e7d5998a7c3"); }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        private class CustomAttributes : ExpireButtonAttributes
        {
            private new LayoutEdit_ZUI Owner => base.Owner as LayoutEdit_ZUI;
            public CustomAttributes(ZuiComponent owner)
            : base(owner)
            {
            }
            protected override string DisplayText
            {
                get
                {
                    return "Modify";
                }
            }

            protected override bool Visible
            {
                get
                {
                    if (Owner.Params.IndexOfInputParam("Run") >= 0)
                    {
                        return false;
                    }
                    return true;
                }
            }

        }
    }
}
 */