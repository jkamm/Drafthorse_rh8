using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Rhinoceros.Drafting.Params;
using Grasshopper.Rhinoceros.Display;
using Grasshopper.Rhinoceros.Drafting;
using Rhino;
using Rhino.Display;
using Rhino.FileIO;
//using Rhino.Geometry;
//using System.CodeDom.Compiler;

using Drafthorse.Component.Base;
using Drafthorse.Helper;

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
                Description = "Hide to show 'Print' Button \n",
                Optional = true,
            }, ParamRelevance.Primary),
            new ParamDefinition(new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport
            {
                Name = "Layout Page",
                NickName = "P",
                Description = "Layout Page(s) to Print\nPages in the same branch print to the same file",
                Optional = false,
                Access = GH_ParamAccess.list,
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_FilePath
            {
                Name = "Folder",
                NickName= "F",
                Description= "Target Folder to Save PDFs \nWill create if it does not exist",
            }, ParamRelevance.Binding),
            new ParamDefinition(new Param_String
            {
                Name = "Filename",
                NickName = "N",
                Description = "Filename \nDefault name is 'Layout'",
                Optional = true,      //This can be true if/when I can set a default value.
            }, ParamRelevance.Primary),
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
                NickName = "R",
                Description = "FilePath on Success",
                Access = GH_ParamAccess.list,
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

        // Added this field at the class level to store persistent results
        private List<string> _persistentResults;
        //= new List<string>() { "Idle" };

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region EscapeBehavior
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            int i = 0;
            bool run = false;
            if (!TryGetData<bool>(DA, inputs[i++].Param.Name, out bool? value0)) run = false;
            if (value0.HasValue) run = value0.Value;

            List<ModelPageViewport> pageList = new List<ModelPageViewport>();
            if (!TryGetDataList<ModelPageViewport>(DA, inputs[i++].Param.Name, out var values1)) return;
            if (values1 != null) pageList = values1.ToList();

            List<int> indexList = pageList.Select(p => (int)p.PageNumber).ToList();

            string folder = String.Empty;
            if (!TryGetData(DA, inputs[i++].Param.Name, out folder)) return;

            string filename = String.Empty;
            if (!TryGetData(DA, inputs[i++].Param.Name, out filename)) filename = "Layout";

            int dpi = 100;
            TryGetData(DA, inputs[i++].Param.Name, out int? value1); 
            if (value1.HasValue) dpi = value1.Value;
            if (dpi > 1200) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Suggested Max DPI = 1200");
            else if (dpi < 72) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Suggested Min DPI = 72");

            int colorMode = 0;
            TryGetData(DA, inputs[i++].Param.Name, out int? value2);
            if (value2.HasValue) colorMode = value2.Value;
            ViewCaptureSettings.ColorMode color = ViewCaptureSettings.ColorMode.BlackAndWhite;
            if (colorMode == 1) color = ViewCaptureSettings.ColorMode.DisplayColor;
            else if (colorMode == 2) color = ViewCaptureSettings.ColorMode.PrintColor;

            bool usePrintWidths = true;
            TryGetData(DA, inputs[i++].Param.Name, out bool? value3);
            if (value3.HasValue) usePrintWidths = value3.Value;

            ObjectDraftingLineWidth lineWidth = new ObjectDraftingLineWidth(0.13);
            TryGetData<ObjectDraftingLineWidth>(DA, inputs[i++].Param.Name, out var value4);
            if(value4 != null) lineWidth = value4;
            double defaultPrintWidth = lineWidth.IsValid ? (double)lineWidth.Width : 0.13;

            double wireScale = 1.0;
            TryGetData(DA, inputs[i++].Param.Name, out double? value5);
            if(value5.HasValue) wireScale = value5.Value;

            bool isRasterMode = false;
            TryGetData(DA, inputs[i++].Param.Name, out bool? value6);
            if(value5.HasValue) isRasterMode = value6.Value;
            RhinoApp.WriteLine("Raster is set to " + isRasterMode);


            bool pressed = (base.Attributes as CustomAttributes).Pressed;

            if (run || pressed)
            {
                List<string> Results = new List<string>();
                if (!System.IO.Directory.Exists(folder))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(folder);
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error creating folder: " + ex.Message);
                        return;
                    }
                }

                FilePdf pdf = FilePdf.Create();
                RhinoDoc rhDoc = RhinoDoc.ActiveDoc;
                RhinoPageView[] pages;
                RhinoPageView[] page_views = rhDoc.Views.GetPageViews();

                if (indexList.Count != 0)
                {
                    pages = new RhinoPageView[indexList.Count];
                    for (int j = 0; j < indexList.Count; j++)
                    {
                        pages[j] = Drafthorse.Helper.Layout.GetPage(indexList[j]);
                    }
                }
                else
                {
                    pages = page_views;
                    Drafthorse.Helper.Layout.SortPagesByPageNumber(pages);
                }

                foreach (RhinoPageView page in pages)
                {
                    double modelToPage = RhinoMath.UnitScale(rhDoc.PageUnitSystem, UnitSystem.Inches);
                    double relativeDPI = ((double)dpi * modelToPage);
                    System.Drawing.Size size = Drafthorse.Helper.Layout.SetSize(page, dpi, modelToPage);
                    Rhino.RhinoApp.WriteLine("Size is w{1}, h{0}, relativeDPI is {2}", size.Width, size.Height, relativeDPI);
                    ViewCaptureSettings settings = new ViewCaptureSettings(page, size, dpi);
                    settings.OutputColor = color;
                    settings.UsePrintWidths = usePrintWidths;
                    settings.DefaultPrintWidthMillimeters = defaultPrintWidth;
                    settings.WireThicknessScale = wireScale;
                    settings.RasterMode = isRasterMode;
                    pdf.AddPage(settings);
                }

                if (!filename.EndsWith(".pdf", true, System.Globalization.CultureInfo.CurrentCulture)) filename += ".pdf";
                string filePath = System.IO.Path.Combine(folder, filename);

                try
                {
                    pdf.Write(filePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        Results.Add("Success");
                        Results.Add(filePath);
                    }
                    else
                    {
                        Results.Add("Failed");
                    }
                }
                catch (Exception ex)
                {
                    Results.Add("Failed: " + ex.Message);
                }

                // Update the persistent results
                _persistentResults = Results;
            }
            //else
            //{
            //    Results.Add("Idle");
            //}

            TrySetDataList(DA, outputs[0].Param.Name, () => _persistentResults);
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
        
        #region Add Value Lists
            protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
            {
                base.AppendAdditionalComponentMenuItems(menu);
                Menu_AppendItem(menu, "Add/Update Layout ValueList", Menu_DoClick, true);
            }

            private void Menu_DoClick(object sender, EventArgs e)
            {
                var pageViews = RhinoDoc.ActiveDoc.Views.GetPageViews();
                Drafthorse.Helper.Layout.SortPagesByPageNumber(pageViews);
                List<string> pageNums = pageViews.Select(p => p.PageNumber.ToString()).ToList();
                List<string> pageNames = pageViews.Select(p => p.PageName.ToString()).ToList();

                int pagesInputIndex = -1;
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].Param.Name == "Pages")
                    {
                        pagesInputIndex = i;
                        break;
                    }
                }

                if (pagesInputIndex >= 0 && !ValList.AddOrUpdateValueList(this, pagesInputIndex, "Layouts", "Layouts To Print: ", pageNames, pageNums))
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + (pagesInputIndex + 1) + "] failed to update");

                ExpireSolution(true);
            }
        #endregion Add Value Lists

        #region AutoValueList
            private bool _handled = false;
            private void SetupEventHandlers()
            {
                if (_handled) return;

                int pagesInputIndex = -1;
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].Param.Name == "Pages")
                    {
                        pagesInputIndex = i;
                        break;
                    }
                }

                if (pagesInputIndex >= 0) Params.Input[pagesInputIndex].ObjectChanged += InputParamChanged;
                _handled = true;
            }

            protected override void BeforeSolveInstance()
            {
                base.BeforeSolveInstance();
                SetupEventHandlers();
            }

            public void InputParamChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
            {
                int pagesInputIndex = -1;
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].Param.Name == "Pages")
                    {
                        pagesInputIndex = i;
                        break;
                    }
                }

                if (pagesInputIndex >= 0 && sender.NickName == Params.Input[pagesInputIndex].NickName)
                {
                    var pageDictionary = RhinoDoc.ActiveDoc.Views.GetPageViews().ToDictionary(v => v.PageName, v => v.PageNumber);
                    List<string> pageViewNames = pageDictionary.Keys.ToList();
                    List<string> layoutIndices = new List<string>();
                    for (int i = 0; i < pageViewNames.Count; i++)
                        layoutIndices.Add(pageDictionary[pageViewNames[i]].ToString());

                    try
                    {
                        ValList.UpdateValueList(this, pagesInputIndex, "Layouts", "Layouts To Print: ", pageViewNames, layoutIndices);
                        ExpireSolution(true);
                    }
                    catch (Exception) { /* Ignore if it's not a ValueList */ }
                }
            }
            #endregion AutoValueList

        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        private class CustomAttributes : ExpireButtonAttributes
        {
            private new PDFLayout_ZUI Owner => base.Owner as PDFLayout_ZUI;

            public CustomAttributes(ZuiComponent owner)
                : base(owner)
            {
            }

            protected override string DisplayText
            {
                get
                {
                    return "Print";
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

        // Reset persistent results when component is reset/recomputed from scratch
        public override void ClearData()
        {
            base.ClearData();
            _persistentResults = new List<string>() { "Idle" };
        }
    }
}
