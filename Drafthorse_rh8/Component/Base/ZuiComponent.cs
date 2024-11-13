using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI.HTML;
using Grasshopper.GUI.Widgets;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Grasshopper.My.Resources;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.CSharp;

namespace Drafthorse.Component.Base
{
  struct NullAttributes : IGH_Attributes
  {
    public PointF Pivot { get => PointF.Empty; set => throw new NotImplementedException(); }
    public RectangleF Bounds { get => RectangleF.Empty; set => throw new NotImplementedException(); }
    public bool AllowMessageBalloon => false;
    public bool HasInputGrip => false;
    public bool HasOutputGrip => false;
    public PointF InputGrip => PointF.Empty;
    public PointF OutputGrip => PointF.Empty;
    public IGH_DocumentObject DocObject => null;
    public IGH_Attributes Parent { get => null; set => throw new NotImplementedException(); }
    public bool IsTopLevel => false;
    public IGH_Attributes GetTopLevel => null;
    public string PathName => string.Empty;
    public Guid InstanceGuid => Guid.Empty;
    public bool Selected { get => false; set => throw new NotImplementedException(); }
    public bool TooltipEnabled => false;
    public void AppendToAttributeTree(List<IGH_Attributes> attributes) { }
    public void ExpireLayout() { }
    public bool InvalidateCanvas(GH_Canvas canvas, GH_CanvasMouseEvent e) => false;
    public bool IsMenuRegion(PointF point) => false;
    public bool IsPickRegion(PointF point) => false;
    public bool IsPickRegion(RectangleF box, GH_PickBox method) => false;
    public bool IsTooltipRegion(PointF canvasPoint) => false;
    public void NewInstanceGuid() => throw new NotImplementedException();
    public void NewInstanceGuid(Guid newID) => throw new NotImplementedException();
    public void PerformLayout() => throw new NotImplementedException();
    public void RenderToCanvas(GH_Canvas canvas, GH_CanvasChannel channel) { }
    public GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, KeyEventArgs e) => GH_ObjectResponse.Ignore;
    public GH_ObjectResponse RespondToKeyUp(GH_Canvas sender, KeyEventArgs e) => GH_ObjectResponse.Ignore;
    public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
    public GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
    public GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
    public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e) => GH_ObjectResponse.Ignore;
    public void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e) { }
    public bool Read(GH_IReader reader) => true;
    public bool Write(GH_IWriter writer) => true;
  }

  public static class GH_PersistentParamExtension
  {
    internal static GH_PersistentParam<T> SetEmptyValue<T>(this GH_PersistentParam<T> param)
        where T : class, IGH_Goo
    {
      param.PersistentData.Clear();
      param.PersistentData.EnsurePath(0);
      return param;
    }

    internal static GH_PersistentParam<T> SetDefaultValue<T>(this GH_PersistentParam<T> param, T value)
        where T : class, IGH_Goo
    {
      param.PersistentData.Clear();
      param.PersistentData.Append(value);
      return param;
    }

    public static GH_PersistentParam<T> SetDefaultValue<T>(this GH_PersistentParam<T> param, object value)
        where T : class, IGH_Goo, new()
    {
      if (!(value is T data))
      {
        data = new T();
        if (!data.CastFrom(value))
          throw new InvalidCastException();
      }

      param.PersistentData.Clear();
      param.PersistentData.Append(data);
      return param;
    }

    public static GH_PersistentParam<T> SetDefaultValue<T>(this GH_PersistentParam<T> param, IGH_Goo value)
        where T : class, IGH_Goo
    {
      if (!(value is T data)) throw new InvalidCastException();

      param.PersistentData.Clear();
      param.PersistentData.Append(data);
      return param;
    }

    public static GH_PersistentParam<T> SetDefaultValues<T>(this GH_PersistentParam<T> param, params object[] value)
        where T : class, IGH_Goo, new()
    {
      var range = value.Select
      (
        x =>
        {
          if (!(x is T data))
          {
            data = new T();
            if (!data.CastFrom(value))
              throw new InvalidCastException();
          }

          return data;
        }
      );

      param.PersistentData.Clear();
      param.PersistentData.AppendRange(range);
      return param;
    }
  }

  public abstract class ZuiComponent : GH_Component, IGH_VariableParameterComponent
  {
    protected ZuiComponent(string name, string nickname, string description, string category, string subCategory)
    : base(name, nickname, description, category, subCategory)
    {
      variableParameterScheme = VariableParameterScheme;
      Params.ParameterSourcesChanged += ParameterSourcesChanged;
    }

    internal static IGH_Param CreateTwin(IGH_Param param)
    {
      var attributes = param.Attributes;
      try
      {
        if (param.Attributes is null)
          param.Attributes = default(NullAttributes);
        
        var newParam = GH_ComponentParamServer.CreateDuplicate(param);
        newParam.NewInstanceGuid();

        if (newParam.MutableNickName && CentralSettings.CanvasFullNames)
          newParam.NickName = newParam.Name;

        // Special cases

        if (param is Param_Number numberParam &&
          newParam is Param_Number newNumberParam)
        {
          newNumberParam.AngleParameter = numberParam.AngleParameter;
          newNumberParam.UseDegrees = numberParam.UseDegrees;
        }

        return newParam;
      }
      finally { param.Attributes = attributes; }
    }

    static IGH_Param CreateSurrogate(IGH_Param param, Type type)
    {
      var attributes = param.Attributes;
      try
      {
        if (param.Attributes is null)
          param.Attributes = default(NullAttributes);
        
        var chunk = new GH_LooseChunk("param");
        if (!param.Write(chunk)) return default;

        var newParam = Activator.CreateInstance(type) as IGH_Param;
        if (!newParam.Read(chunk)) return default;

        if (newParam.MutableNickName && CentralSettings.CanvasFullNames)
          newParam.NickName = newParam.Name;

        return newParam;
      }
      finally { param.Attributes = attributes; }
    }

    static bool IsGenericSubclassOf(Type type, Type baseGenericType)
    {
      for (; type != typeof(object); type = type.BaseType)
      {
        var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (baseGenericType == cur)
          return true;
      }
      return false;
    }

    protected enum ParamRelevance
    {
      Binding = int.MaxValue,
      Primary = Binding - 1,
      Secondary = Binding - 2,
      Tertiary = Binding - 3,
      Quarternary = Binding - 4,
      Quinary = Binding - 5,
      Senary = Binding - 6,
      Septenary = Binding - 7,
      Occasional = Binding - 8,
      None = default,
    }

    protected readonly struct ParamDefinition
    {
      public readonly IGH_Param Param;
      public readonly ParamRelevance Relevance;
      public readonly object Tag;

      public ParamDefinition(IGH_Param param)
      {
        Param = param;
        Relevance = ParamRelevance.Binding;
        Tag = default;
      }

      public ParamDefinition(IGH_Param param, ParamRelevance relevance)
      {
        Param = param;
        Relevance = relevance;
        Tag = default; 
      }

      public ParamDefinition(IGH_Param param, ParamRelevance relevance, object tag)
      {
        Param = param;
        Relevance = relevance;
        Tag = tag; 
      }

      public static ParamDefinition Create<T>(string name, string nickname, string description = "", GH_ParamAccess access = GH_ParamAccess.item, bool optional = false, ParamRelevance relevance = ParamRelevance.Binding)
        where T : class, IGH_Param, new()
      {
        var param = new T()
        {
          Name = name,
          NickName = nickname,
          Description = description,
          Access = access,
          Optional = optional
        };

        return new ParamDefinition(param, relevance);
      }

      public static ParamDefinition Create<T>(string name, string nickname, string description, object defaultValue, GH_ParamAccess access = GH_ParamAccess.item, bool optional = false, ParamRelevance relevance = ParamRelevance.Binding)
        where T : class, IGH_Param, new()
      {
        var param = new T()
        {
          Name = name,
          NickName = nickname,
          Description = description,
          Access = access,
          Optional = optional
        };

        if (IsGenericSubclassOf(typeof(T),typeof(GH_PersistentParam<>)))
        {
          dynamic persistentParam = param;
          persistentParam.SetPersistentData(defaultValue);
        }

        return new ParamDefinition(param, relevance);
      }
    }

    protected abstract ParamDefinition[] Inputs { get; }
    protected sealed override void RegisterInputParams(GH_InputParamManager manager)
    {
      foreach (var definition in Inputs.Where(x => x.Relevance >= ParamRelevance.Primary))
        manager.AddParameter(CreateTwin(definition.Param));
    }

    protected abstract ParamDefinition[] Outputs { get; }
    protected sealed override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      foreach (var definition in Outputs.Where(x => x.Relevance >= ParamRelevance.Primary))
        manager.AddParameter(CreateTwin(definition.Param));
    }

    private void ParameterSourcesChanged(object sender, GH_ParamServerEventArgs e)
    {
      if (e.ParameterSide != GH_ParameterSide.Input) return;
      var t = IndexOf(Inputs, e.Parameter.Name); if (t < 0) return;

      if (Inputs[t].Relevance == ParamRelevance.Binding && Inputs[t].Param.Optional)
      {
        // Optional parameters enable gap logic and provoke undesired nulls at the output.
        e.Parameter.Optional = e.Parameter.SourceCount == 0;
      }
    }

    #region Cluster Support
    IEnumerable<IGH_ActiveObject> TopLevelObjects
    {
      get
      {
        IGH_ActiveObject documentObject = this;
        do
        {
          yield return documentObject;
          documentObject = documentObject.OnPingDocument()?.Owner as IGH_ActiveObject;

        } while (documentObject is object);
      }
    }

    protected IGH_ActiveObject TopLevelObject => TopLevelObjects.LastOrDefault();

    protected void ExpireTopDisplay(bool redraw) => TopLevelObject.OnDisplayExpired(redraw);
    protected void ExpireTopSolution(bool recompute)
    {
      var topLevelObject = TopLevelObject;
      if (topLevelObject == this) ExpireSolution(recompute);
      else
      {
        ExpireSolution(false);
        topLevelObject.ExpireSolution(recompute);
      }

      topLevelObject.OnDisplayExpired(false);
    }

    public override void AddRuntimeMessage(GH_RuntimeMessageLevel level, string text)
    {
      base.AddRuntimeMessage(level, text);

      foreach (var top in TopLevelObjects.Skip(1))
        top.AddRuntimeMessage(level, text);
    }
    #endregion

    #region UI
    ParamDefinition GetMostRelevantParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      int begin = -1, end = templateParams.Length;
      if (componentParams.Count > 0)
      {
        if (index <= 0)
        {
          end = IndexOf(templateParams, componentParams[0].Name);
        }
        else if (index >= componentParams.Count)
        {
          begin = IndexOf(templateParams, componentParams[componentParams.Count - 1].Name);
        }
        else
        {
          begin = IndexOf(templateParams, componentParams[index - 1].Name);
          end = IndexOf(templateParams, componentParams[index].Name);
        }
      }

      ParamDefinition mostRelevat = default;

      begin = Math.Max(-1, begin);
      end = Math.Min(end, templateParams.Length);

      for (int i = begin + 1; i < end; ++i)
      {
        var definition = templateParams[i];
        if (definition.Relevance >= ParamRelevance.Occasional && definition.Relevance > mostRelevat.Relevance)
          mostRelevat = definition;
      }

      return mostRelevat;
    }

    public virtual IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var template = GetMostRelevantParameter(side, index);
      if (template.Relevance != ParamRelevance.None)
        return CreateTwin(template.Param);

      return default;
    }

    public virtual bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return CanRemoveParameter(side, index);
    }

    public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return GetMostRelevantParameter(side, index).Relevance != ParamRelevance.None;
    }

    public virtual bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      var t = IndexOf(templateParams, componentParams[index].Name);
      return t < 0 || templateParams[t].Relevance != ParamRelevance.Binding;
    }

    /// <summary>
    /// This function will get called before an attempt is made to add binding parameters.
    /// </summary>
    /// <param name="side">Parameter side.</param>
    /// <param name="index">Insertion index of parameter.</param>
    /// <returns>Return True if your component needs a parameter at the given location.</returns>
    public virtual bool ShouldInsertParameter(GH_ParameterSide side, int index)
    {
      return GetMostRelevantParameter(side, index) is ParamDefinition template &&
             template.Relevance == ParamRelevance.Binding;
    }

    /// <summary>
    /// This function will get called before an attempt is made to remove obsolete parameters.
    /// </summary>
    /// <param name="side">Parameter side.</param>
    /// <param name="index">Removal index of parameter.</param>
    /// <returns>Return True if your component does not support the parameter at the given location.</returns>
    public virtual bool ShouldRemoveParameter(GH_ParameterSide side, int index)
    {
      var templateParams = side == GH_ParameterSide.Input ? Inputs : Outputs;
      var componentParams = side == GH_ParameterSide.Input ? Params.Input : Params.Output;

      return IndexOf(templateParams, componentParams[index].Name) < 0;
    }

    public virtual void VariableParameterMaintenance() { }

    void CanvasFullNamesChanged()
    {
      void UpdateName(IEnumerable<IGH_Param> values, ParamDefinition[] template)
      {
        int i = 0;
        foreach (var value in values)
        {
          while (i < template.Length && value.Name != template[i].Param.Name) ++i;

          if (i >= template.Length)
            break;

          if (value.MutableNickName)
          {
            if (CentralSettings.CanvasFullNames)
            {
              if (value.NickName == template[i].Param.NickName)
                value.NickName = template[i].Param.Name;
            }
            else
            {
              if (value.NickName == template[i].Param.Name)
                value.NickName = template[i].Param.NickName;
            }
          }
        }
      }

      UpdateName(Params.Input, Inputs);
      UpdateName(Params.Output, Outputs);
    }
    #endregion

    #region Display
    private struct UpdateParamsButton
    {
      public enum Icon
      {
        None,
        Remove,
        Insert,
        Cross,
        Collapse,
        Expand
      }

      public const int ZoomLevel = 5;
      public const float RadiusMin = 1.5F;
      public const float RadiusMax = 3.0F;

      private readonly ZuiAttributes m_attributes;
      private readonly Icon m_icon;
      public RectangleF Bounds;

      public UpdateParamsButton(ZuiAttributes attributes, Icon icon, PointF center)
      {
        m_attributes = attributes;
        m_icon = icon;
        Bounds = new RectangleF(center.X - RadiusMax, center.Y - RadiusMax, 2 * RadiusMax, 2 * RadiusMax);
      }

      public bool Contains(PointF pt)
      {
        if (!Bounds.Contains(pt))
          return false;

        float xm = 0.5F * (Bounds.Left + Bounds.Right);
        float ym = 0.5F * (Bounds.Top + Bounds.Bottom);

        return GH_GraphicsUtil.Distance(pt, new PointF(xm, ym)) <= RadiusMax;
      }
      
      GH_PaletteStyle CreateButtonStyle(GH_PaletteStyle parent, int alpha)
      {
        Color fill = Color.Black;
        Color edge = Color.Black;
        Color text = Color.White;

        switch (m_icon)
        {
          case Icon.Collapse:
          case Icon.Remove:
          case Icon.Cross:
            fill = Color.White;
            edge = Color.Black;
            text = Color.Black;
            break;

          case Icon.Expand:
          case Icon.Insert:
            fill = Color.Black;
            edge = Color.Black;
            text = Color.White;
            break;
        }
        return new GH_PaletteStyle(Color.FromArgb(alpha, fill), Color.FromArgb(alpha, edge), Color.FromArgb(alpha, text));
      }

      public void Render(Graphics graphics, PointF cursor, int alpha)
      {
        float xm = Bounds.X + 0.5F * Bounds.Width;
        float ym = Bounds.Y + 0.5F * Bounds.Height;

        var d_range = new Interval(RadiusMax, RadiusMax * 3);
        var s_range = new Interval(RadiusMax, RadiusMin);

        float d = GH_GraphicsUtil.Distance(new PointF(xm, ym), cursor);
        double t = d_range.NormalizedParameterAt(d);
        float r = (float) s_range.ParameterAt(t);
        r = Math.Min(r, RadiusMax);
        r = Math.Max(r, RadiusMin);

        Bounds = new RectangleF(xm - r, ym - r, 2 * r, 2 * r);

        var palette = GH_CapsuleRenderEngine.GetImpliedPalette(m_attributes.Owner);
        var capsuleStyle = GH_CapsuleRenderEngine.GetImpliedStyle(palette, m_attributes);
        var buttonStyle = CreateButtonStyle(capsuleStyle, alpha);

        using (var fill = new SolidBrush(buttonStyle.Fill))
          graphics.FillEllipse(fill, Bounds);

        using (var edge = new Pen(buttonStyle.Edge, 0.5F))
          graphics.DrawEllipse(edge, Bounds);

        float sz = 0.5F * r;
        float sw = 0.25F * r;
        float sf = (sz + 0.25f) * 0.75f;
        using (var symbol = new Pen(buttonStyle.Text, sw))
        {
          switch (m_icon)
          {
            case Icon.None:
              // don't draw the icon
              break;
            case Icon.Insert:
              graphics.DrawLine(symbol, xm - sz, ym, xm + sz, ym);
              graphics.DrawLine(symbol, xm, ym - sz, xm, ym + sz);
              break;
            case Icon.Remove:
              graphics.DrawLine(symbol, xm - sz, ym, xm + sz, ym);
              break;
            case Icon.Cross:
              sz -= 0.25F;
              graphics.DrawLine(symbol, xm - sz, ym - sz, xm + sz, ym + sz);
              graphics.DrawLine(symbol, xm - sz, ym + sz, xm + sz, ym - sz);
              break;
            case Icon.Collapse:
              sz += 0.25f;
              graphics.DrawLines
              (
                symbol,
                new PointF[]
                {
                  new PointF(xm - sf, ym + sz * 0.4f),
                  new PointF(xm, ym - sz * 0.4f),
                  new PointF(xm + sf, ym + sz * 0.4f)
                }
              );
              break;
            case Icon.Expand:
              sz += 0.25f;
              graphics.DrawLines
              (
                symbol, 
                new PointF[]
                {
                  new PointF(xm - sf, ym - sz * 0.4f),
                  new PointF(xm, ym + sz * 0.4f),
                  new PointF(xm + sf, ym - sz * 0.4f)
                }
              );
              break;

          }
        }
      }
    }

    protected class ZuiAttributes : GH_ComponentAttributes
    {
      public ZuiAttributes(ZuiComponent owner) : base(owner) { }

      bool CanvasFullNames = CentralSettings.CanvasFullNames;

      UpdateParamsButton ShowParamsButton;
      UpdateParamsButton HideParamsButton;

      public override void ExpireLayout()
      {
        if (CanvasFullNames != CentralSettings.CanvasFullNames)
        {
          if (Owner is ZuiComponent zuiComponent)
            zuiComponent.CanvasFullNamesChanged();
          
          CanvasFullNames = CentralSettings.CanvasFullNames;
        }

        ShowParamsButton = default;
        HideParamsButton = default;

        base.ExpireLayout();
      }

      protected override void Layout()
      {
        base.Layout();

        var midX = ContentBox.X + (ContentBox.Width * 0.5f);
        HideParamsButton = new UpdateParamsButton(this, UpdateParamsButton.Icon.Collapse, new PointF(midX, ContentBox.Y));
        ShowParamsButton = new UpdateParamsButton(this, UpdateParamsButton.Icon.Expand, new PointF(midX, ContentBox.Y + ContentBox.Height));
      }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        switch (channel)
        {
          case GH_CanvasChannel.Objects:
            var bounds = Bounds;
            if (!canvas.Viewport.IsVisible(ref bounds, 10))
              return;

            base.Render(canvas, graphics, channel);

            if (Owner is ZuiComponent component)
            {
              int alpha = GH_Canvas.ZoomFadeHigh;
              if (alpha >= UpdateParamsButton.ZoomLevel)
              {
                PointF cursor = canvas.PointToClient(Cursor.Position);
                cursor = canvas.Viewport.UnprojectPoint(cursor);

                if (!component.AreAllParametersVisible())
                  ShowParamsButton.Render(graphics, cursor, alpha);

                if (!component.AreAllParametersConnected())
                  HideParamsButton.Render(graphics, cursor, alpha);
              }
            }
            return;

          case GH_CanvasChannel.Overlay:

            if (Owner.Phase == GH_SolutionPhase.Computing)
              RenderProgress(canvas, graphics);

            break;
        }

        base.Render(canvas, graphics, channel);
      }

      public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (Owner is ZuiComponent zuiComponent && e.Button == MouseButtons.Left)
        {
          if (GH_Canvas.ZoomFadeHigh >= UpdateParamsButton.ZoomLevel)
          {
            if (ShowParamsButton.Contains(e.CanvasLocation))
            {
              zuiComponent.ShowAllParameters(sender, e);
              return GH_ObjectResponse.Handled;
            }

            if (HideParamsButton.Contains(e.CanvasLocation))
            {
              zuiComponent.HideUnconnectedParameters(sender, e);
              return GH_ObjectResponse.Handled;
            }
          }
        }

        return base.RespondToMouseDown(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (sender.Viewport.Zoom >= GH_Viewport.ZoomDefault * 0.6f)
        {
          bool ctrl = Control.ModifierKeys == Keys.Control;
          bool shift = Control.ModifierKeys == Keys.Shift;

          if (e.Button == MouseButtons.Left && (ctrl || shift))
          {
            if (Owner is ZuiComponent zuiComponent)
            {
              sender.ActiveInteraction = null;
              if (ctrl) zuiComponent.HideUnconnectedParameters(sender, e);
              else if (shift) zuiComponent.ShowAllParameters(sender, e);

              return GH_ObjectResponse.Handled;
            }
          }
        }

        return base.RespondToMouseDoubleClick(sender, e);
      }

      public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
      {
        if (GH_Canvas.ZoomFadeHigh >= UpdateParamsButton.ZoomLevel)
        {
          if (HideParamsButton.Contains(canvasPoint))
          {
            //e.Icon = Res_ObjectIcons.RemoveParameter_24x24;
            e.Title = "Hide Parameters";
            e.Text = "Hide all unused parameters.\nCtrl + Dbl Click";
            return;
          }

          if (ShowParamsButton.Contains(canvasPoint))
          {
            //e.Icon = Res_ObjectIcons.InsertParameter_24x24;
            e.Title = "Show Parameters";
            e.Text = "Show all available parameters.\nShift + Dbl Click";
            return;
          }
        }

        base.SetupTooltip(canvasPoint, e);
      }

      #region Computing
      string ComputingMessage;
      Color ComputingColor;
      int ComputingProgress;
      int ComputingTotal;
      long LastUpdateMoment = long.MinValue;

      private RectangleF ProgressBounds(GH_Viewport viewport)
      {
        var offset = 0.0f;
        if (viewport.Zoom > 1.6f && !string.IsNullOrEmpty(Owner.Message)) offset += 16.0f;
        if (GH_ProfilerWidget.SharedVisible && Owner.ProcessorTime.Milliseconds >= GH_ProfilerWidget.Threshold) offset += 16.0f;
        var bounds = Bounds;

        return new RectangleF
        (
          bounds.Left,
          bounds.Bottom + 4.0f + offset,
          bounds.Width,
          string.IsNullOrEmpty(ComputingMessage) ? 10 : 14
        );
      }

      protected void RenderProgress(GH_Canvas canvas, Graphics graphics)
      {
        if (canvas.Viewport.Zoom < 0.90f) return;

        var bounds = ProgressBounds(canvas.Viewport);
        if (!canvas.Viewport.IsVisible(ref bounds, 0.0f))
          return;

        var progressRect = bounds;
        progressRect.Inflate(-3, 0);

        using (var totalPath = CreateRoundedRectanglePath(progressRect, 2))
        using (var totalBrush = new SolidBrush(Color.FromArgb(50, 77, 0)))
        {
          graphics.FillPath(totalBrush, totalPath);
        }

        if (ComputingProgress > 0 && ComputingTotal > 0)
        {
          progressRect.Inflate(-1, -1);
          progressRect.Width = progressRect.Width * ComputingProgress / ComputingTotal;

          using (var progressPath = CreateRoundedRectanglePath(progressRect, 2))
          using (var progressBrush = new SolidBrush(ComputingColor))
          {
            graphics.FillPath(progressBrush, progressPath);
          }
        }

        if (!string.IsNullOrEmpty(ComputingMessage))
        {
          var foreground = GH_GraphicsUtil.OffsetColour(ComputingColor, ComputingColor.GetBrightness() < 0.4 ? +100 : -100);
          using (var totalBrush = new SolidBrush(foreground))
          {
            var format = new StringFormat
            {
              Alignment = StringAlignment.Center,
              LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(ComputingMessage, GH_FontServer.StandardAdjusted, totalBrush, bounds, format);
          }
        }
      }

      private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float cornerRadius)
      {
        var roundedRect = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = cornerRadius * 2.0f;
        var size = new SizeF(diameter, diameter);
        var arc = new RectangleF(rect.Location, size);

        // Top left arc
        roundedRect.AddArc(arc, 180, 90);

        // Top right arc
        arc.X = rect.Right - diameter;
        roundedRect.AddArc(arc, 270, 90);

        // Bottom right arc
        arc.Y = rect.Bottom - diameter;
        roundedRect.AddArc(arc, 0, 90);

        // Bottom left arc
        arc.X = rect.Left;
        roundedRect.AddArc(arc, 90, 90);

        roundedRect.CloseFigure();
        return roundedRect;
      }

      public void StartProgressMeter(int total, string message)
      {
        LastUpdateMoment = Environment.TickCount;
        ComputingMessage = message;
        ComputingTotal = total;
        ComputingProgress = 0;
        ComputingColor = Color.FromArgb(140, 235, 0);

        Owner.OnDisplayExpired(false);
      }

      public void StopProgressMeter()
      {
        ComputingColor = Color.Empty;
        ComputingProgress = 0;
        ComputingTotal = 0;
        ComputingMessage = null;
        LastUpdateMoment = long.MinValue;

        Owner.OnDisplayExpired(false);
      }

      public bool UpdateProgressMeter(int step = 1, bool absolute = false)
      {
        if (absolute) ComputingProgress = step;
        else ComputingProgress += step;

        if (ComputingProgress < 0) ComputingProgress = 0;
        if (ComputingProgress > ComputingTotal) ComputingProgress = ComputingTotal;

        var CurrentUpdateTime = Environment.TickCount;
        if (CurrentUpdateTime - LastUpdateMoment > 250)
        {
          LastUpdateMoment = CurrentUpdateTime;

          if (Instances.ActiveCanvas is GH_Canvas canvas)
          {
            var bounds = ProgressBounds(canvas.Viewport);
            if (canvas.Viewport.IsVisible(ref bounds, 0.0f))
              Owner.OnDisplayExpired(true);
          }

          return !GH_Document.IsEscapeKeyDown();
        }

        return true;
      }
      #endregion
    }

    protected abstract class ExpireButtonAttributes : ZuiAttributes
    {
      protected Rectangle ButtonBounds { get; set; }

      public bool Pressed { get; private set; } = false;

      protected abstract string DisplayText { get; }
      protected abstract bool Visible { get; }

      public ExpireButtonAttributes(ZuiComponent owner) : base(owner) { }

      protected override void Layout()
      {
        base.Layout();

        if (Visible)
        {
          Rectangle newBounds = GH_Convert.ToRectangle(Bounds);
          newBounds.Height += 22;

          Rectangle buttonBounds = newBounds;
          buttonBounds.Y = buttonBounds.Bottom - 22;
          buttonBounds.Height = 22;
          buttonBounds.Inflate(-2, -2);

          Bounds = (RectangleF)newBounds;
          ButtonBounds = buttonBounds;
        }
        else ButtonBounds = Rectangle.Empty;
      }

      protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
      {
        base.Render(canvas, graphics, channel);

        if (Visible && channel == GH_CanvasChannel.Objects)
        {
          using (var ghCapsule = Pressed ?
            GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Grey, DisplayText, 2, 0) :
            GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, DisplayText, 2, 0))
            ghCapsule.Render(graphics, Selected, Owner.Locked, false);
        }
      }

      public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (Pressed)
        {
          if (((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
          {
            if (Owner.OnPingDocument() is GH_Document document)
            {
              GH_Document.SolutionEndEventHandler SolutionEnd = null;
              document.SolutionEnd += SolutionEnd = (object s, GH_SolutionEventArgs args) =>
              {
                (s as GH_Document).SolutionEnd -= SolutionEnd;
                Pressed = false;
              };
            }
            Owner.ExpireSolution(true);
          }

          Pressed = false;
          sender.Refresh();
          return GH_ObjectResponse.Release;
        }

        return base.RespondToMouseUp(sender, e);
      }

      public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
      {
        if (Visible && e.Button == MouseButtons.Left && ((RectangleF)ButtonBounds).Contains(e.CanvasLocation))
        {
          Pressed = true;
          sender.Refresh();
          return GH_ObjectResponse.Capture;
        }

        return base.RespondToMouseDown(sender, e);
      }
    }

    public override void CreateAttributes() => Attributes = new ZuiAttributes(this);

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      if (Inputs.Any(x => x.Relevance != ParamRelevance.Binding) || Outputs.Any(x => x.Relevance != ParamRelevance.Binding))
      {
        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Hide unused parameters", HideUnconnectedParameters, !AreAllParametersConnected(), false);
        Menu_AppendItem(menu, "Show all parameters", ShowAllParameters, !AreAllParametersVisible(), false);
      }
    }

    bool AreAllParametersVisible()
    {
      return Params.Input.Count == Inputs.Length && Params.Output.Count == Outputs.Length;
    }

    bool AreAllParametersConnected()
    {
      for (int i = 0; i < Params.Input.Count; ++i)
      {
        var param = Params.Input[i];
        if (param.DataType > GH_ParamData.@void) continue;
        if (CanRemoveParameter(GH_ParameterSide.Input, i))
          return false;
      }

      for (int o = 0; o < Params.Output.Count; ++o)
      {
        var param = Params.Output[o];
        if (param.Recipients.Count > 0) continue;
        if (CanRemoveParameter(GH_ParameterSide.Output, o))
          return false;
      }

      return true;
    }

    void ShowAllParameters(object sender, EventArgs e)
    {
      if (OnPingDocument() is GH_Document document)
      {
        RecordUndoEvent("Show all parameters");

        bool inputAdded = false;
        {
          for (int index = 0; index <= Params.Input.Count; ++index)
          {
            while (CanInsertParameter(GH_ParameterSide.Input, index))
            {
              var param = CreateParameter(GH_ParameterSide.Input, index);
              inputAdded |= Params.RegisterInputParam(param, index);
            }
          }
        }

        bool outputAdded = false;
        {
          for (int index = 0; index <= Params.Output.Count; ++index)
          {
            while (CanInsertParameter(GH_ParameterSide.Output, index))
            {
              var param = CreateParameter(GH_ParameterSide.Output, index);
              outputAdded |= Params.RegisterOutputParam(param, index);
            }
          }
        }

        Params.OnParametersChanged();

        if (inputAdded || outputAdded)
        {
          VariableParameterMaintenance();
          ExpireSolution(true);
        }
        else OnDisplayExpired(false);
      }
    }

    void HideUnconnectedParameters(object sender, EventArgs e)
    {
      if (OnPingDocument() is GH_Document document)
      {
        RecordUndoEvent("Hide unconnected parameters");

        bool inputRemoved = false;
        {
          int index = 0;
          foreach (var input in Params.Input.ToArray())
          {
            if
            (
              input.DataType > GH_ParamData.@void ||
              !CanRemoveParameter(GH_ParameterSide.Input, index)
            )
            {
              ++index;
            }
            else if (Params.UnregisterInputParameter(input))
            {
              inputRemoved |= true;
            }
          }
        }

        {
          int index = 0;
          foreach (var output in Params.Output.ToArray())
          {
            if (output.Recipients.Count > 0 || !CanRemoveParameter(GH_ParameterSide.Output, index))
              ++index;
            else
              Params.UnregisterOutputParameter(output);
          }
        }

        Params.OnParametersChanged();

        if (inputRemoved)
        {
          VariableParameterMaintenance();
          ExpireSolution(true);
        }
        else OnDisplayExpired(false);
      }
    }
    #endregion

    #region Help
    protected override string HtmlHelp_Source()
    {
      string text = HelpDescription + "<BR><BR><HR>" + Environment.NewLine;
      text += GenerateParameterHelp();

      var formatter = new GH_HtmlFormatter(this, Name, text);
      formatter.AddRemark($"This component is a ZUI component. Zoom in on it to enable the parameters managment UI visible.");

      return formatter.HtmlFormat();
    }

    protected new string GenerateParameterHelp()
    {
      string text = string.Empty;
      if (Inputs.Length > 0)
      {
        text += $"Input parameters: <BR>{Environment.NewLine}<dl>{Environment.NewLine}";
        foreach (var item in Inputs) text += GenerateParameterHelp(item.Param);
        text += $"</dl> <BR>{Environment.NewLine}";
      }

      if (Outputs.Length > 0)
      {
        text += $"Output parameters: <BR>{Environment.NewLine}<dl>{Environment.NewLine}";
        foreach (var item in Outputs) text += GenerateParameterHelp(item.Param);
        text += $"</dl> <BR>{Environment.NewLine}";
      }

      return text;
    }

    private new string GenerateParameterHelp(IGH_Param param)
    {
      var description = $"<dt><b> {param.Name} </b><i> ({param.TypeName})</i></dt>{Environment.NewLine}";
      description += $"<dd> {param.Description} </dd>{Environment.NewLine}";
      return description;
    }
    #endregion

    #region IO
    protected static int IndexOf(IReadOnlyList<ParamDefinition> list, string name)
    {
      for (int i = 0; i < list.Count; ++i)
      {
        if (list[i].Param.Name == name)
          return i;
      }

      return -1;
    }

    readonly struct ParamComparer : IComparer<IGH_Param>
    {
      readonly ParamDefinition[] ReferenceList;

      public ParamComparer(ParamDefinition[] referenceList) => ReferenceList = referenceList;
      public int Compare(IGH_Param x, IGH_Param y) => IndexOf(ReferenceList, x.Name) - IndexOf(ReferenceList, y.Name);
    }

    public override void AddedToDocument(GH_Document document)
    {
      // If we read from a different version some parameters may need to be adjusted.
      if (VariableParameterScheme is string currentParameterScheme && currentParameterScheme != variableParameterScheme)
      {
        document.DestroyObjectTable();

        // PerformLayout here to obtain parameters pivots.
        Attributes.PerformLayout();

        // Detach Obsolete parameters.
        {
          var unknownParameters = new List<IGH_Param>();

          for (var inputIndex = Params.Input.Count - 1; inputIndex >= 0; --inputIndex)
          {
            if (!ShouldRemoveParameter(GH_ParameterSide.Input, inputIndex))
              continue;

            var input = Params.Input[inputIndex];
            var y = input.Attributes.Pivot.Y;
            Params.UnregisterInputParameter(input, false);
            input.IconDisplayMode = GH_IconDisplayMode.name;
            input.Optional = false;
            input.Attributes = default;
            input.CreateAttributes();
            input.Attributes.Pivot = new System.Drawing.PointF(Attributes.Bounds.Left + input.Attributes.Bounds.Width / 2.0f, y);
            unknownParameters.Add(input);
          }

          for (var outputIndex = Params.Output.Count - 1; outputIndex >= 0; --outputIndex)
          {
            if (!ShouldRemoveParameter(GH_ParameterSide.Output, outputIndex))
              continue;

            var output = Params.Output[outputIndex];
            var y = output.Attributes.Pivot.Y;
            Params.UnregisterOutputParameter(output, false);
            output.IconDisplayMode = GH_IconDisplayMode.name;
            output.Optional = false;
            output.Attributes = default;
            output.CreateAttributes();
            output.Attributes.Pivot = new System.Drawing.PointF(Attributes.Bounds.Right - output.Attributes.Bounds.Width / 2.0f, y);
            unknownParameters.Add(output);
          }

          // Add unknown Parameters to the document to keep as much
          // previous information as possible available to the user.
          // Input parameters may contain PersistentData.
          if (unknownParameters.Count > 0)
          {
            var previousVersion = string.IsNullOrWhiteSpace(variableParameterScheme) ? new Version(0, 0, 0, 0) : new System.Reflection.AssemblyName(variableParameterScheme).Version;
            var currentVersion = string.IsNullOrWhiteSpace(currentParameterScheme) ? new Version(0, 0, 0, 0) : new System.Reflection.AssemblyName(currentParameterScheme).Version;

            var action = "Mutated";
            if (previousVersion is object && currentVersion is object)
            {
              if (previousVersion < currentVersion) action = "Upgraded";
              else if (previousVersion > currentVersion) action = "Downgraded";
            }

            var index = document.Objects.IndexOf(this);
            var group = new Grasshopper.Kernel.Special.GH_Group
            {
              NickName = $"{action} : {Name}", // We tag it to allow user find those groups.
              Border = Grasshopper.Kernel.Special.GH_GroupBorder.Blob,
              Colour = System.Drawing.Color.FromArgb(211, GH_Skin.palette_warning_standard.Fill)
            };
            document.AddObject(group, false, index++);

            group.AddObject(InstanceGuid);
            foreach (var param in unknownParameters)
            {
              param.Locked = true;
              if (document.AddObject(param, false, index++))
                group.AddObject(param.InstanceGuid);
            }
          }
        }

        // Refresh paremeters with current types & values.
        {
          foreach (var input in Inputs)
          {
            var index = Params.IndexOfInputParam(input.Param.Name);
            if (index >= 0)
            {
              var param = Params.Input[index];
              var inputType = input.Param.GetType();
              if (inputType != param.GetType() && CreateSurrogate(param, inputType) is IGH_Param surrogate)
              {
                GH_UpgradeUtil.MigrateSources(param, surrogate);
                Params.UnregisterInputParameter(param);
                Params.RegisterInputParam(surrogate, index);
                param = surrogate;
              }

              param.Access = input.Param.Access;
              param.Optional = input.Param.Optional;

              if (input.Param is Param_Number input_number && param is Param_Number param_number)
              {
                param_number.AngleParameter = input_number.AngleParameter;
                param_number.UseDegrees = input_number.UseDegrees;
              }
            }
          }

          foreach (var output in Outputs)
          {
            var index = Params.IndexOfOutputParam(output.Param.Name);
            if (index >= 0)
            {
              var param = Params.Output[index];
              var outputType = output.Param.GetType();
              if (outputType != param.GetType() && CreateSurrogate(param, outputType) is IGH_Param surrogate)
              {
                GH_UpgradeUtil.MigrateRecipients(param, surrogate);
                Params.UnregisterOutputParameter(param);
                Params.RegisterOutputParam(surrogate, index);
                param = surrogate;
              }

              param.Access = output.Param.Access;
              param.Optional = output.Param.Optional;

              if (output.Param is Param_Number output_number && param is Param_Number param_number)
              {
                param_number.AngleParameter = output_number.AngleParameter;
                param_number.UseDegrees = output_number.UseDegrees;
              }
            }
          }
        }

        // Sort Parameters in Inputs & Outputs order.
        {
          Params.Input.Sort(new ParamComparer(Inputs));
          Params.Output.Sort(new ParamComparer(Outputs));
        }

        // Add Binding Parameters.
        {
          for (int i = 0; i <= Params.Input.Count; ++i)
          {
            while (ShouldInsertParameter(GH_ParameterSide.Input, i))
              Params.RegisterInputParam(CreateParameter(GH_ParameterSide.Input, i), i);
          }

          for (int i = 0; i <= Params.Output.Count; ++i)
          {
            while (ShouldInsertParameter(GH_ParameterSide.Output, i))
              Params.RegisterOutputParam(CreateParameter(GH_ParameterSide.Output, i), i);
          }
        }

        // Update Common fields
        if (Activator.CreateInstance(GetType()) is IGH_Component prototype)
        {
          Name = prototype.Name;
          NickName = CentralSettings.CanvasFullNames ? prototype.Name : prototype.NickName;
          Description = prototype.Description;
          Category = prototype.Category;
          SubCategory = prototype.SubCategory;
        }

        // ExpireLayout here in case we have removed, added or sorted parameters.
        Attributes.ExpireLayout();

        // Mark component as converted
        variableParameterScheme = currentParameterScheme;
      }

      base.AddedToDocument(document);
    }

    /// <summary>
    /// Scheme name for the current input and output parameters configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default implentation returns a simplified version of component Type
    /// <see cref="System.Reflection.Assembly.FullName"/>
    /// to ensure parameters are synchronized with the current component implementation.
    /// </para>
    /// <para>
    /// For a more accurate and manual control of upgrade-downgrade mechanism
    /// subtypes may override this property and return a constant value related
    /// to the current component implementation version.
    /// </para>
    /// <para>
    /// Automatic upgrade-downgrade may be disabled returning null here.
    /// </para>
    /// </remarks>
    protected virtual string VariableParameterScheme
    {
      get
      {
        var assembly = GetType().Assembly;
        if (assembly.IsDynamic) return null;

        var assemblyName = assembly.GetName();
        var scheme = assemblyName.Name;

        if (assemblyName.Version is object)
          scheme += $", Version={assemblyName.Version}";

        if (!string.IsNullOrWhiteSpace(assemblyName.CultureName))
          scheme += $", Culture={assemblyName.CultureName}";

        return scheme;
      }
    }
    string variableParameterScheme;

    public override bool Read(GH_IReader reader)
    {
      if (!base.Read(reader)) return false;

      // Upgrade from non IGH_VariableParameterComponent data
      if (!reader.ChunkExists("ParameterData"))
      {
        // Inputs
        {
          // Tentatively register all parameters
          foreach (var definition in Inputs)
            Params.RegisterInputParam(CreateTwin(definition.Param));

          var found = new bool[Params.Input.Count];
          int index = 0;
          var chunk = default(GH_IReader);
          while ((chunk = reader.FindChunk("param_input", index++)) is object)
          {
            var name = string.Empty;
            if (chunk.TryGetString("Name", ref name))
            {
              var i = Params.IndexOfInputParam(name);
              if (i < 0) continue;
              var param = Params.Input[i];

              var access = param.Access;
              var optional = param.Optional;
              param.Read(chunk);
              param.Optional = optional;
              param.Access = access;

              found[i] = true;
            }
          }

          // Remove not-found parameters
          for (int i = Params.Input.Count - 1; i >= 0; --i)
          {
            if (!found[i] && CanRemoveParameter(GH_ParameterSide.Input, i))
            {
              var param = Params.Input[i];
              Params.UnregisterInputParameter(param);
            }
          }
        }

        // Outputs
        {
          // Tentatively register all parameters
          foreach (var definition in Outputs)
            Params.RegisterOutputParam(CreateTwin(definition.Param));

          var found = new bool[Params.Output.Count];
          int index = 0;
          var chunk = default(GH_IReader);
          while ((chunk = reader.FindChunk("param_output", index++)) is object)
          {
            var name = string.Empty;
            if (chunk.TryGetString("Name", ref name))
            {
              var o = Params.IndexOfOutputParam(name);
              if (o < 0) continue;
              var param = Params.Output[o];

              var access = param.Access;
              var optional = param.Optional;
              param.Read(chunk);
              param.Optional = optional;
              param.Access = access;

              found[o] = true;
            }
          }

          // Remove not-found parameters
          for (int o = Params.Output.Count - 1; o >= 0; --o)
          {
            if (!found[o] && CanRemoveParameter(GH_ParameterSide.Output, o))
            {
              var param = Params.Output[o];
              Params.UnregisterOutputParameter(param);
            }
          }
        }

        VariableParameterMaintenance();
      }

      // Read parameters-scheme value
      if (!reader.TryGetString("VariableParameterScheme", ref variableParameterScheme))
        variableParameterScheme = default;

      return true;
    }

    public override bool Write(GH_IWriter writer)
    {
      if (!base.Write(writer)) return false;

      // Write parameters-scheme value
      if (variableParameterScheme is object)
        writer.SetString("VariableParameterScheme", variableParameterScheme);

      return true;
    }
    #endregion

    #region Parameters

    static int IndexOf(IList<IGH_Param> list, string name, out IGH_Param value)
    {
      value = default;
      int index = 0;
      for (; index < list.Count; ++index)
      {
        var item = list[index];
        if (item.Name == name)
        {
          value = item;
          return index;
        }
      }

      return -1;
    }

    protected static T Input<T>(GH_ComponentParamServer parameters, string name)
      where T : class, IGH_Param
    {
      if (IndexOf(parameters.Input, name, out var value) >= 0)
        return value as T;

      return default;
    }

    protected static T Output<T>(GH_ComponentParamServer parameters, string name)
      where T : class, IGH_Param
    {
      if (IndexOf(parameters.Output, name, out var value) >= 0)
        return value as T;

      return default;
    }
    #endregion

    #region DataAccess

    protected static bool AnyHasValue(params object[] values) => values.Any(x => x is object);

    internal protected bool TryGetData<T>(IGH_DataAccess DA, string name, out T value) where T : class
    {
      value = default;

      var index = Params.IndexOfInputParam(name);
      if (index >= 0 && Params.Input[index].VolatileData.PathCount > 0)
      {
        if (!DA.GetData(index, ref value)) { value = default; return false; }
        return true;
      }

      return true;
    }

    internal protected bool TryGetData<T>(IGH_DataAccess DA, string name, out T? value) where T : struct
    {
      value = default;

      var index = Params.IndexOfInputParam(name);
      if (index >= 0 && Params.Input[index].VolatileData.PathCount > 0)
      {
        if (typeof(T).IsEnum)
        {
          var goo = default(IGH_Goo);
          if (!DA.GetData(index, ref goo)) { value = default; return false; }
          if (GH_Convert.ToInt32(goo, out var enum_index, GH_Conversion.Both))
          {
            value = (T)Enum.ToObject(typeof(T), enum_index);
          }
          else if (GH_Convert.ToString(goo, out var enum_name, GH_Conversion.Both))
          {
            try { value = (T)Enum.Parse(typeof(T), enum_name); }
            catch (ArgumentException) { }
          }
        }
        else if (!DA.GetData(index, ref value)) { value = default; return false; }
        return true;
      }

      return true;
    }

    internal protected bool TrySetData<T>(IGH_DataAccess DA, string name, Func<T> value)
    {
      var index = Params.IndexOfOutputParam(name);
      return index >= 0 && DA.SetData(index, value());
    }

    protected bool GetDataList<T>(IGH_DataAccess DA, string name, out T[] values) where T : class
    {
      var index = Params.IndexOfInputParam(name);
      if (index >= 0 && Params.Input[index].VolatileData.PathCount > 0)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos)
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              else if (goo.CastTo(out data)) values[i] = data;
              else if (typeof(T).IsEnum && goo is GH_Integer integer) values[i] = (T)(object)integer.Value;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo)Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T)TGoo;
              }
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return false;
    }

    protected bool GetDataList<T>(IGH_DataAccess DA, string name, out T?[] values) where T : struct
    {
      var index = Params.IndexOfInputParam(name);
      if (index >= 0 && Params.Input[index].VolatileData.PathCount > 0)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T?[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos)
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              else if (goo.CastTo(out data)) values[i] = data;
              else if (typeof(T).IsEnum && goo is GH_Integer integer) values[i] = (T) (object) integer.Value;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo)Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T)TGoo;
              }
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return false;
    }

    protected bool TryGetDataList<T>(IGH_DataAccess DA, string name, out T[] values)
    {
      var index = Params.IndexOfInputParam(name);
      if (index >=0 && Params.Input[index].VolatileData.PathCount > 0)
      {
        var goos = new List<IGH_Goo>();
        if (DA.GetDataList(index, goos))
        {
          values = new T[goos.Count];

          var TIsGoo = typeof(IGH_Goo).IsAssignableFrom(typeof(T));

          int i = 0;
          foreach (var goo in goos)
          {
            if (goo != null)
            {
              if (goo is T data) values[i] = data;
              else if (goo.CastTo(out data)) values[i] = data;
              else if (TIsGoo)
              {
                var TGoo = (IGH_Goo)Activator.CreateInstance(typeof(T));
                if (TGoo.CastFrom(goo))
                  values[i] = (T)TGoo;
              }
            }

            i++;
          }

          return true;
        }
      }

      values = default;
      return true;
    }

    protected bool TrySetDataList<T>(IGH_DataAccess DA, string name, Func<IEnumerable<T>> values)
    {
      var index = Params.IndexOfOutputParam(name);
      return index >= 0 && DA.SetDataList(index, values());
    }

    readonly struct DataTree<T> : IGH_DataTree
    {
      readonly GH_Path Path;
      readonly IEnumerable<IEnumerable<T>> Enumerable;
      public DataTree(GH_Path path, IEnumerable<IEnumerable<T>> enumerable)
      {
        Path = path;
        Enumerable = enumerable;
      }

      bool IGH_DataTree.MergeWithParameter(IGH_Param param)
      {
        if (param is null)
          return false;

        if (Enumerable is null)
          return param.AddVolatileDataList(Path.AppendElement(0), default);

        int index = 0;
        foreach (var enumerable in Enumerable)
          param.AddVolatileDataList(Path.AppendElement(index++), enumerable);

        return true;
      }
    }

    internal bool TrySetDataTree(IGH_DataAccess DA, string name, Func<IEnumerable<IEnumerable>> tree)
    {
      return TrySetDataTree(DA, name, () => tree().Select(x => x?.Cast<object>()));
    }

    protected bool TrySetDataTree<T>(IGH_DataAccess DA, string name, Func<IEnumerable<IEnumerable<T>>> tree)
    {
      var index = Params.IndexOfOutputParam(name);
      if (index < 0) return false;

      return DA.SetDataTree(index, new DataTree<T>(DA.ParameterTargetPath(index).AppendElement(DA.ParameterTargetIndex(index)), tree()));
    }
    protected bool TrySetDataTree<T>(IGH_DataAccess DA, string name, Func<IEnumerable<IEnumerable<T?>>> tree)
      where T : struct
    {
      var index = Params.IndexOfOutputParam(name);
      if (index < 0) return false;

      return DA.SetDataTree(index, new DataTree<T?>(DA.ParameterTargetPath(index).AppendElement(DA.ParameterTargetIndex(index)), tree()));
    }
    #endregion
  }
}
