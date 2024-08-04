using System;
using System.Collections.Generic;
using System.Drawing;
using Drafthorse_rh8.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;


public class Param_DetailView : GH_PersistentGeometryParam<GH_DetailView>, IGH_BakeAwareObject
{
    public Param_DetailView()
    : base(new GH_InstanceDescription("DetailView", "DV", "Represents a Rhino Detail View", "Drafthorse", "Detail")) 
    {
        //_hidden = false;
    }
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("613BD246-3828-426A-8812-2C07A63CF386");
    protected override Bitmap Icon => Resources.DetailViewParam; // Provide an icon

    RhinoDoc doc = RhinoDoc.ActiveDoc;

    protected override GH_GetterResult Prompt_Plural(ref List<GH_DetailView> values)
    {
        // Prompt user to select multiple DetailViews
        values = new List<GH_DetailView>();
        var obj_ref = new Rhino.Input.Custom.GetObject();
        obj_ref.SetCommandPrompt("Select detail views");
        obj_ref.GeometryFilter = ObjectType.Detail;
        obj_ref.GetMultiple(1, 0);
        var res = obj_ref.Result();
        if (res != Rhino.Input.GetResult.Object)
            return GH_GetterResult.cancel;

        for (int i = 0; i < obj_ref.ObjectCount; i++)
        {
            var rhinoObj = obj_ref.Object(i).Object();
            if (rhinoObj is DetailViewObject detailViewObject)
            {
                var detailView = detailViewObject.DetailGeometry;
                var ghDetailView = new GH_DetailView(detailView);
                values.Add(ghDetailView);
            }
        }
        return GH_GetterResult.success;
    }

    protected override GH_GetterResult Prompt_Singular(ref GH_DetailView value)
    {
        // Prompt user to select a single DetailView
        value = null;
        var obj_ref = new Rhino.Input.Custom.GetObject();
        obj_ref.SetCommandPrompt("Select a detail view");
        obj_ref.GeometryFilter = ObjectType.Detail;
        var res = obj_ref.Get();
        if (res != Rhino.Input.GetResult.Object)
            return GH_GetterResult.cancel;

        var rhinoObj = obj_ref.Object(0).Object();
        if (rhinoObj is DetailViewObject detailViewObject)
        {
            value = new GH_DetailView(detailViewObject.DetailGeometry);
            return GH_GetterResult.success;
        }
        return GH_GetterResult.cancel;
    }  
    

    #region IGH_BakeAwareObject Implementation
    public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
    {
        BakeGeometry(doc, null, obj_ids);
    }

    public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> objectIds)
    {
        if (att == null)
        {
            att = doc.CreateDefaultAttributes();
        }
        GH_BakeUtility gH_BakeUtility = new GH_BakeUtility(OnPingDocument());
        gH_BakeUtility.BakeObjects(m_data, att, doc);
        objectIds.AddRange(gH_BakeUtility.BakedIds);
    }

    public bool IsBakeCapable => true;
    #endregion
}
