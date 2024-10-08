﻿using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace Drafthorse.Component.Layout.List3
{
    public class LayoutDelete : Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the DeleteLayout class.
        /// </summary>
        public LayoutDelete()
          : base("Delete Layout", "DeleteLayout",
              "Delete a Layout using its Index (use Layout Index to get index from name) \nWARNING: THIS CANNOT BE UNDONE! ",
              "Drafthorse", "Layout")
        {
            ButtonName = "Delete";
        }
        int in_page;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var bToggleParam = new Params.Param_BooleanToggle();
            Params.Input[pManager.AddParameter(bToggleParam, "Run", "R", "run using an input", GH_ParamAccess.item)].Optional = true;
            var pageParam = new Grasshopper.Rhinoceros.Display.Params.Param_ModelPageViewport();
            in_page = pManager.AddParameter(pageParam, "Page", "P", "Layout Page(s) to delete", GH_ParamAccess.tree);
            //Params.Input[pManager.AddIntegerParameter("Layout Index", "Li[]", "Indices for Layouts to delete \nAdd ValueList to get Layouts", GH_ParamAccess.tree)].Optional = true;
            pManager[1].DataMapping = GH_DataMapping.Flatten;
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Done", "Done", "action completed", GH_ParamAccess.item);
            //Don't know why this isn't working, but it's not.  Something to do with int type
            //pManager.AddIntegerParameter("Deleted", "Del", "Layouts deleted", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region EscapeBehavior
            //Esc behavior code snippet from 
            // http://james-ramsden.com/you-should-be-implementing-esc-behaviour-in-your-grasshopper-development/
            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }
            #endregion EscapeBehavior

            bool run = false;
            DA.GetData("Run", ref run);

            List<int> uniqIndexList = new List<int>();

            bool Done = false;

            var allPages = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Rhinoceros.Display.ModelPageViewport>();
            DA.GetDataTree("Page", out allPages);
            var pageList = allPages.FlattenData();


            // Compiled Params
            //var index = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Integer>();
            //DA.GetDataTree("Layout Index", out index);

            //goal: Always flatten inputs - add flatten to node

            //var indexList = index.FlattenData();
            HashSet<int> indexSet = new HashSet<int>();
            // replace with HashSet of pageviews?
            HashSet<Grasshopper.Rhinoceros.Display.ModelPageViewport> pageSet = new HashSet<Grasshopper.Rhinoceros.Display.ModelPageViewport>(pageList);

            for (int i = 0; i < pageList.Count; i++)
            {
                int thisInt;
                GH_Convert.ToInt32(pageList[i].PageNumber, out thisInt, GH_Conversion.Both);
                if (indexSet.Add(thisInt))
                {
                    uniqIndexList.Add(thisInt);
                }
            }


            uniqIndexList.Sort();
            uniqIndexList.Reverse();

            if (run || Execute)
            {
                string warning = indexSet.Count == 1
                    ? "Delete Layout?"
                    : "Delete Layouts?";

                
                var result2 = overrideMessage == false
                    ? MessageBox.Show("WARNING: this cannot be undone \nright click on component to hide this message",
                    warning, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    : DialogResult.Yes;
                if (result2 == DialogResult.Yes)
                {
                    for (int j = 0; j < indexSet.Count; j++)
                    {
                        Rhino.Display.RhinoPageView pageView = Helper.Layout.GetPage(uniqIndexList[j]);
                        pageView.Close();
                    }
                    /*

                    foreach (Grasshopper.Rhinoceros.Display.ModelPageViewport page in pageList) 
                    {
                        Rhino.Display.RhinoPageView pageView = Helper.Layout.GetPage((int)page.PageNumber);
                        if (pageView != null) pageView.Close();
                    }
                    */
                   

                    Done = true;
                    Execute = false;
                   
                }
                DA.SetData(0, Done);
                //Not currently working
                //DA.SetData("Deleted", uniqIndexList.ToArray());
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.LayoutDelete_bitmap;


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0475b324-b3e1-4a72-a240-82a9210b43a9"); }
        }


        #region Override Message
        //implements a menu item that is preserved on the component, along with undo/redo capability
        private bool overrideMessage = false;

        public bool Override
        {
            get { return overrideMessage; }
            set
            {
                overrideMessage = value;
                if (overrideMessage)
                {
                    Message = "warning off";
                }
                else
                {
                    Message = null;
                }
            }
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            //Menu_AppendItem(menu, "Add/Update ValueList", Menu_DoClick);
            //Menu_AppendSeparator(menu);
            ToolStripMenuItem item = Menu_AppendItem(menu, "override warning", Menu_OverrideClicked, true, Override);
            item.ToolTipText = "select to disable pop-up window warning.";
        }
        private void Menu_OverrideClicked(object sender, EventArgs e)
        {
            //if createFolders is true, turn to false and if false, turn to true.
            RecordUndoEvent("Override");
            Override = !Override;
            ExpireSolution(true);
        }
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("Override", Override);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Override = reader.GetBoolean("Override");
            // Then call the base class implementation.
            return base.Read(reader);
        }
        #endregion

        /*
        #region Add Value Lists

        private void Menu_DoClick(object sender, EventArgs e)
        {
            var pageDictionary = Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews().ToDictionary(v => v.PageName, v => v.PageNumber);
            List<string> pageViewNames = pageDictionary.Keys.ToList();
            List<string> layoutIndices = new List<string>();
            for (int i = 0; i < pageViewNames.Count; i++)
                layoutIndices.Add(pageDictionary[pageViewNames[i]].ToString());

            if (!AddOrUpdateValueList(this, 1, "Layouts", "To Delete: ", pageViewNames, layoutIndices))
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ValueList at input [" + 1 + "] failed to update");

            ExpireSolution(true);
        }
        #endregion Add Value Lists

        #region AutoValueList

        //Update a value list if added to a given input(based on Elefront and FabTools)
        //on event for a source added to a given input

        private bool _handled = false;

        private void SetupEventHandlers()
        {
            if (_handled)
                return;

            Params.Input[1].ObjectChanged += InputParamChanged;

            _handled = true;
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            SetupEventHandlers();
        }


        public void InputParamChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {

            //optional feedback
            //if (e.Type == GH_ObjectEventType.Sources)
            //    Rhino.RhinoApp.WriteLine("Sources changed on input '{0}'.", sender.NickName);

            if (sender.NickName == Params.Input[1].NickName) 
            {
                // optional feedback
                // Rhino.RhinoApp.WriteLine("This is the right input");

                var pageDictionary = Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews().ToDictionary(v => v.PageNumber.ToString(), v => v.PageName);
                List<string> pageViewNames = pageDictionary.Keys.ToList();
                List<string> layoutIndices = new List<string>();
                for (int i = 0; i < pageViewNames.Count; i++)
                    layoutIndices.Add(pageDictionary[pageViewNames[i]].ToString());

                //try to modify input as a valuelist
                try
                {
                    UpdateValueList(this, 1, "Layouts", "To Delete: ", pageViewNames, layoutIndices);
                    ExpireSolution(true);
                }
                //if it's not a value list, ignore
                catch (Exception) { };
            }
        }


        #endregion AutoValueList
         */
    }
}
