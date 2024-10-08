﻿using Drafthorse.Helper;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;


namespace Drafthorse.Component.Layout.List3
{
    public class LayoutEditText : Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the ModifyText class.
        /// </summary>
        public LayoutEditText()
          : base("Modify Text", "LOText",
              "Replace text of Named Text Objects on a Layout with new text",
              "Drafthorse", "Layout")
        {
            ButtonName = "Modify";
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        int in_page;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "run using an input", GH_ParamAccess.item, false);
            //pManager.AddIntegerParameter("Index", "Li", "Indexed Layout to change", GH_ParamAccess.item);
            var pageParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            in_page = pManager.AddParameter(pageParam, "Layout Page", "P", "Input Layout Page", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Names of Text Objects to replace", GH_ParamAccess.list);
            pManager.AddTextParameter("Text", "T", "New Text for Text Objects", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Success or Failure for each text", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData("Run", ref run);

            var thisPage = new Grasshopper.Rhinoceros.Display.ModelPageViewport();
            DA.GetData(in_page, ref thisPage);
            
            //DA.GetData("Layout", ref name);

            List<string> textKeys = new List<string>();
            DA.GetDataList("Name", textKeys);

            List<string> textVals = new List<string>();
            DA.GetDataList("Text", textVals);

            if (thisPage.PageNumber == null) return;
            int index = (int)thisPage.PageNumber;

            #region EscapeBehavior
            //Esc behavior code snippet from 
            // http://james-ramsden.com/you-should-be-implementing-esc-behaviour-in-your-grasshopper-development/
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            List<Rhino.Commands.Result> results = new List<Rhino.Commands.Result>();

            if (run || Execute)
            {
                //goal: Test validity of all input before processing?  Currently will copy a template before producing name error.

                //Check that Keys and Values have same length
                if (textKeys.Count == textVals.Count)
                {
                    Rhino.Display.RhinoPageView page = Helper.Layout.GetPage(index);
                    results = Helper.Layout.ReplaceText(textKeys, textVals, page);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Must have equal number of Keys and Values");
                }
                DA.SetDataList("Result", results);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutText_bitmap;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d39b9aaf-eaf1-48ce-8570-9ef6bfefaeac"); }
        }
    }
}