using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Drafthorse.Component.UserText
{
    public class DocTextEdit : Base.DH_ButtonComponent
    {
        /// <summary>
        /// Initializes a new instance of the DocTextEdit class.
        /// </summary>
        public DocTextEdit()
          : base("Edit Document Text", "EditDocText",
              "Get and/or Set Key/Value pairs to the Document Text Table. \nStrings starting with '.' are hidden from users",
              "Drafthorse", "User Text")
        {
            ButtonName = "Write";
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var bToggleParam = new Params.Param_BooleanToggle();
            Params.Input[pManager.AddParameter(bToggleParam, "Run", "R", "run using an input", GH_ParamAccess.item)].Optional = true;
            Params.Input[pManager.AddTextParameter("Keys", "K", "Keys for Doc Texts (unique)", GH_ParamAccess.list)].Optional = true;
            Params.Input[pManager.AddTextParameter("Values", "V", "Values for Doc Texts (must match Key count)", GH_ParamAccess.list)].Optional = true;
            /*
            pManager.AddIntegerParameter("Method", "M", "Write Method (overrides Component Menu)", GH_ParamAccess.list);
            Params.Input[3].Optional = true;

            Param_Integer obj = (Param_Integer)pManager[3];
            obj.AddNamedValue("Ensure", 0);
            obj.AddNamedValue("Update", 1);
            obj.AddNamedValue("Merge", 2);
            obj.AddNamedValue("Replace", 3);
            obj.AddNamedValue("Remove", 4);
             */

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Result", "R", "Result of Write operation", GH_ParamAccess.list);
            pManager.AddTextParameter("Keys", "K", "Keys written to Document", GH_ParamAccess.list);
            pManager.AddTextParameter("Values", "V", "Values written to Document", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.DocObjects.Tables.StringTable docText = Rhino.RhinoDoc.ActiveDoc.Strings;
            string[] allKeys = new string[docText.Count];

            for (int i = 0; i < docText.Count; i++) allKeys[i] = docText.GetKey(i);
            
            List<string> newKeys = new List<string>();
            DA.GetDataList("Keys", newKeys);

            List<string> newVals = new List<string>();
            DA.GetDataList("Values", newVals);

            bool run = false;
            if (!DA.GetData("Run", ref run)) run = false;

            // Check that there are the same number of Keys and Values
            if (newKeys.Count != newVals.Count) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Key/Value Pairs do not match. Double-check the data structure");

            if (run || Execute)
            {
                switch (wMethod)
                {
                    case WriteMethod.Ensure:
                        {
                            for (int i = 0; i < newKeys.Count; i++)
                                EnsureKV(docText, allKeys, newKeys[i], newVals[i]);
                            break;
                        }
                    case WriteMethod.Update:
                        {
                            for (int i = 0; i < newKeys.Count; i++)
                                UpdateKV(docText, allKeys, newKeys[i], newVals[i]);
                            break;
                        }
                    case WriteMethod.Merge:
                        {
                            for (int i = 0; i < newKeys.Count; i++)
                                MergeKV(docText, newKeys[i], newVals[i]);
                            break;
                        }
                    case WriteMethod.Replace:
                        {
                            foreach (var key in allKeys)
                                docText.Delete(key); 
                            for (int i = 0; i < newKeys.Count; i++)
                                docText.SetString(newKeys[i], newVals[i]);
                            break;
                        }
                    case WriteMethod.Remove:
                        {
                            for (int i = 0; i < newKeys.Count; i++)
                                RemoveKV(docText, newKeys[i], newVals[i]);
                            break;
                        }
                }
                
            }

            docText = Rhino.RhinoDoc.ActiveDoc.Strings;

            string[] allKeysPost = new string[docText.Count];
            string[] allVals = new string[docText.Count];

            for (int i = 0; i < docText.Count; i++)
            {
                allKeysPost[i] = docText.GetKey(i);
                allVals[i] = docText.GetValue(i);
            }
            DA.SetDataList("Keys", allKeysPost.ToList());
            DA.SetDataList("Values", allVals.ToList());
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Drafthorse_rh8.Properties.Resources.SetDocText;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9ee69741-b39c-40dc-810f-4d3f8bce16f4"); }
        }

       public bool EnsureKV(Rhino.DocObjects.Tables.StringTable docText, string[] allKeys, string key, string val)
        {
            string message = String.Empty;
            if (Array.IndexOf(allKeys, key) == -1)
            {
                docText.SetString(key, val);
                return true;
            }
            message = "Key not found, KV pair not added";
            return false;
        }

        public bool UpdateKV(Rhino.DocObjects.Tables.StringTable docText, string[] allKeys, string key, string val)
        {
            //string message = String.Empty;
            if (Array.IndexOf(allKeys, key) != -1)
            {
                docText.SetString(key, val);
                return true;
            }
            //message = "Key not found, KV pair not updated";
            return false;
        }

        public bool MergeKV(Rhino.DocObjects.Tables.StringTable docText, string key, string val)
        {
            docText.SetString(key, val);
            return true;
        }
        
        public bool RemoveKV(Rhino.DocObjects.Tables.StringTable docText, string key, string val)
        {
            //string message = String.Empty;
            if (docText.GetValue(key) == val)
            {
                docText.Delete(key);
                //return new Tuple<bool, string>(true, message);
                return true;
            }
            //message = "Key/Value Pair not found";
            //return new Tuple<bool, string>(false, message);
            return false;
        }

        #region write method
        //implements a menu item that is preserved on the component, along with undo/redo capability
        enum WriteMethod
        {
            Ensure,
            Update,
            Merge,
            Replace,
            Remove
        }
        private Enum writemethod = WriteMethod.Merge;

        public Enum wMethod
        { 
            get => writemethod;
            set
            {
                writemethod = value;
                Message = writemethod.ToString();
            }
        }
       
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendSeparator(menu);
            ToolStripMenuItem item1 = Menu_AppendItem(menu, "Ensure", Menu_EnsureClicked, true, @checked: wMethod.Equals(WriteMethod.Ensure));
            item1.ToolTipText = "When selected, the key/value pair(s) will be merged with the existing document user text collection." +
                "\nIf a key already exists in the collection, the value will not be updated.";
            ToolStripMenuItem item2 = Menu_AppendItem(menu, "Update", Menu_UpdateClicked, true, @checked: wMethod.Equals(WriteMethod.Update));
            item2.ToolTipText = "When selected, the key/value pair(s) will be merged with the existing document user text collection." +
                "\nOnly if a key already exists in the collection, the value will be updated.";
            ToolStripMenuItem item3 = Menu_AppendItem(menu, "Merge", Menu_MergeClicked, true, @checked: wMethod.Equals(WriteMethod.Merge));
            item3.ToolTipText = "When selected, the key/value pair(s) will be merged with the existing document user text collection." +
                "\nIf a key already exists in the collection, the value will be updated.";
            ToolStripMenuItem item4 = Menu_AppendItem(menu, "Replace", Menu_ReplaceClicked, true, @checked: wMethod.Equals(WriteMethod.Replace));
            item4.ToolTipText = "When selected, all existing key/value pair(s) document user text items in the collection will be removed before setting the new key/value pair(s).";
            ToolStripMenuItem item5 = Menu_AppendItem(menu, "Remove", Menu_RemoveClicked, true, @checked: wMethod.Equals(WriteMethod.Remove));
            item5.ToolTipText = "When selected, any key/value pair(s) matching the input key and value will be removed from the document user text collection";

        }

        private void Menu_EnsureClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("WriteMethod"); 
            wMethod = WriteMethod.Ensure;
            ExpireSolution(true);
        }
        private void Menu_UpdateClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("WriteMethod");
            wMethod = WriteMethod.Update;
            ExpireSolution(true);
        }
        private void Menu_MergeClicked(object sender, EventArgs e) 
        {
            RecordUndoEvent("WriteMethod");
            wMethod = WriteMethod.Merge;
            ExpireSolution(true);
        }
        private void Menu_ReplaceClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("WriteMethod");
            wMethod = WriteMethod.Replace;
            ExpireSolution(true);
        }
        private void Menu_RemoveClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("WriteMethod");
            wMethod = WriteMethod.Remove;
            ExpireSolution(true);
        }
                        
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetString("WriteMethod", wMethod.ToString());
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Enum.TryParse(reader.GetString("WriteMethod"), out WriteMethod wMethod);
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /*
         */
        protected override void BeforeSolveInstance()
        {
            // sets the message to display immediately according to default
            base.BeforeSolveInstance();
            wMethod = writemethod;
        }
        #endregion




    }
}