using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Drafthorse.Component.Base;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Rhinoceros.Drafting.Params;

namespace Layout
{
    public class PDFLayout_ZUI : ZuiComponent
    {
        private static readonly ParamDefinition[] inputs = new ParamDefinition[10]
        {
            new ParamDefinition(new Param_Boolean
            {
                Name = "Run",
                NickName = "R",
                Description = "Hide to show 'Modify' Button\nBeware of creating update loops with 'Query Pages'",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
            {
                Name = "Layout Page",
                NickName = "P",
                Description = "Input Layout Page",
                Optional = false,
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_FilePath
            {
                Name = "Folder",
                NickName= "F",
                Description= "Target Folder to Save PDFs \nWill create if it does not exist",
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_String
            {
                Name = "FileName",
                NickName = "N",
                Description = "Filename",
                //Optional = true,      //This can be true if/when I can set a default value.
            }, ParamRelevance.Binding),
            new ParamDefinition (new Param_Integer
            {
                Name="DPI",
                NickName="D",
                Description="Print Resolution (72-1200) Default is 100",
                Optional= true,
            }, ParamRelevance.Secondary),
            new ParamDefinition (new Param_Integer
            {
                Name="ColorMode",
                NickName="C",
                Description="0 = Black&White\n1 = Display Color\n2 = Print Color",
                Optional= true,
            }, ParamRelevance.Tertiary),
            new ParamDefinition(new Param_Boolean
            {
                Name="UsePrintWidths",
                NickName="U",
                Description="Use defined print widths (False prints Display values)",
                Optional = true,
            }, ParamRelevance.Quarternary),
            new ParamDefinition(new Param_ObjectDraftingLineWidth
            {
                Name="DraftingLineWidth",
                NickName="D",
                Description="Set default print width for undefined lines\nOnly values used",
                Optional=true,
            }, ParamRelevance.Quinary),
            new ParamDefinition(new Param_Number
            {
                Name="WireScale",
                NickName="W",
                Description="Scale width of curves in print",
                Optional=true,
            }, ParamRelevance.Senary),
            new ParamDefinition(new Param_Boolean
            {
                Name="Raster/Vector",
                NickName="R/V",
                Description="Set to True to print in Raster Mode",
                Optional=true,
            },ParamRelevance.Septenary)
        };

        private static readonly ParamDefinition[] outputs = new ParamDefinition[1]
        {
            new ParamDefinition(new Param_String
            {
                Name="Result",
                NickName = "N",
                Description = "FilePath on Success"
            },ParamRelevance.Primary)
        };

        protected override ParamDefinition[] Inputs => inputs;
        protected override ParamDefinition[] Outputs => outputs;

        /// <summary>
        /// Initializes a new instance of the PDFLayout_ZUI class.
        /// </summary>
        public PDFLayout_ZUI()
          : base("PDF Layout ZUI", "DH pdf",
              "Print one or more Layouts to one or more PDFs",
              "Drafthorse", "Layout")
        {
        }

        //public override GH_Exposure Exposure => GH_Exposure.quarternary;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutPDF;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C5DE1855-1B75-4FA7-A21A-B334BB922C05"); }
        }

        public override void VariableParameterMaintenance()
        {
            base.VariableParameterMaintenance();
            int num = base.Params.IndexOfInputParam("DPI");
            if (num >= 0 && base.Params.Input[num] is Param_Integer param_Integer)
            {
                param_Integer.ClearNamedValues();
                param_Integer.AddNamedValue("Low (100)", 100);
                param_Integer.AddNamedValue("Medium (600)", 600);
                param_Integer.AddNamedValue("High (1200)", 1200);                
            }

            int num2 = base.Params.IndexOfInputParam("ColorMode");
            if (num2 >= 0 && base.Params.Input[num2] is Param_Integer param_Integer2)
            {
                param_Integer2.ClearNamedValues();
                param_Integer2.AddNamedValue("Black + White", 0);
                param_Integer2.AddNamedValue("Display Color", 1);
                param_Integer2.AddNamedValue("Print Color",2);


            }
        }

    }
}