using System;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using AntDesign.Core.Helpers;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OneOf;

namespace AntDesign
{

    public partial class RangeItem : AntInputComponentBase<(double, double)>
    {
        private const string PreFixCls = "ant-multi-range-slider";
        private HtmlElement _sliderDom;
        private ElementReference _leftHandle;
        private ElementReference _rightHandle;
        private string _leftHandleCssPosition = "left: 0%; right: auto; transform: translateX(-50%);";
        private string _rightHandleCssPosition = "left: 0%; right: auto; transform: translateX(-50%);";
        private string _trackCssPosition = "left: 0%; width: 0%; right: auto;";
        private bool _isFocused;
        private string _focusClass = "";
        private string _leftFocusZIndex = "z-index: 2;";
        private string _rightFocusZIndex = "z-index: 2;";
        private bool _mouseDown;
        private bool _mouseDownOnTrack;
        private bool _right = true;
        private bool _isInitialized = false;
        private double _initialLeftValue;
        private double _initialRightValue;
        private Tooltip _toolTipRight;
        private Tooltip _toolTipLeft;
        private bool _customStyleChange;
        private string _customTrackStyle = "";
        private string _customDescriptionStyle = "";
        private string _customFocusStyle = "";
        private string _focusStyle = "";
        private string _customEdgeBorderStyle = "";
        private bool _isDataSet;

        protected static readonly EventCallbackFactory CallbackFactory = new EventCallbackFactory();
        /// <summary>
        /// Used to evaluate if OnAfterChange needs to be called
        /// </summary>
        private (double, double) _valueCache;

        /// <summary>
        /// Used to figure out how much to move left and right when range is moved
        /// </summary>
        double _distanceToLeftHandle;
        double _distanceToRightHandle;
        internal bool IsRangeDragged { get; set; }
        internal bool HasAttachedEdge
        {
            get => _hasAttachedEdge;
            set
            {
                _hasAttachedEdge = value;
                Parent.HasAttachedEdges = value;
            }
        }

        internal bool HasAttachedEdgeWithGap
        {
            get => _hasAttachedEdgeWithGap;
            set
            {
                _hasAttachedEdgeWithGap = value;
                HasAttachedEdge = value;

            }
        }
        internal RangeEdge AttachedHandleNo { get; set; }
        internal RangeEdge HandleNoRequestingAttaching { get; set; }
        private string _attachedLeftHandleClass = "";
        private string _attachedRightHandleClass = "";
        internal RangeItem AttachedItem { get; set; }
        internal Action ChangeAttachedItem { get; set; }
        internal double GapDistance { get; private set; }
        internal bool Master { get; set; }
        internal bool Slave { get; set; }

        private string RightHandleStyleFormat
        {
            get
            {
                if (Parent.Reverse)
                {
                    if (Parent.Vertical)
                    {
                        return "bottom: auto; top: {0};";
                    }
                    else
                    {
                        return "right: {0}; left: auto; transform: translateX(50%);";
                    }
                }
                else
                {
                    if (Parent.Vertical)
                    {
                        return "top: auto; bottom: {0}; transform: translateY(50%);";
                    }
                    else
                    {
                        return "left: {0}; right: auto; transform: translateX(-50%);";
                    }
                }
            }
        }

        private string LeftHandleStyleFormat
        {
            get
            {
                if (Parent.Reverse)
                {
                    if (Parent.Vertical)
                    {
                        return "bottom: auto; top: {0};";
                    }
                    else
                    {
                        return "right: {0}; left: auto; transform: translateX(50%);";
                    }
                }
                else
                {
                    if (Parent.Vertical)
                    {
                        return "top: auto; bottom: {0}; transform: translateY(50%);";
                    }
                    else
                    {
                        return "left: {0}; right: auto; transform: translateX(-50%);";
                    }
                }
            }
        }

        private string TrackStyleFormat
        {
            get
            {
                if (Parent.Reverse)
                {
                    if (Parent.Vertical)
                    {
                        return "bottom: auto; height: {1}; top: {0};";
                    }
                    else
                    {
                        return "right: {0}; width: {1}; left: auto;";
                    }
                }
                else
                {
                    if (Parent.Vertical)
                    {
                        return "top: auto; height: {1}; bottom: {0};";
                    }
                    else
                    {
                        return "left: {0}; width: {1}; right: auto;";
                    }
                }
            }
        }

        [Inject]
        private IDomEventListener DomEventListener { get; set; }

        #region Parameters
        [CascadingParameter(Name = "Range")]
        public MultiRangeSlider Parent { get => _parent; set => _parent = value; }

        [Parameter]
        public IRangeItemData Data { get; set; }

        /// <summary>
        /// The default value of slider. When <see cref="Range"/> is false, use number, otherwise, use [number, number]
        /// </summary>
        [Parameter]
        public (double, double) DefaultValue { get; set; }

        //TODO: check with mixing of Disabled in Range and in parent
        /// <summary>
        /// If true, the slider will not be intractable
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        private bool _hasToolTip = true;
        private bool _hasToolTipSet;

        [Parameter]
        public bool HasTooltip
        {
            get => _hasToolTip;
            set
            {
                _hasToolTip = value;
                _hasToolTipSet = true;
            }
        }

        /// <summary>
        /// The maximum value the slider can slide to
        /// </summary>
        public double Max => Parent.Max;

        /// <summary>
        /// The minimum value the slider can slide to
        /// </summary>

        public double Min => Parent.Min;

        private double _leftValue = double.MinValue;

        internal double LeftValue
        {
            get => _leftValue;
            set
            {
                double candidate = value;
                if (_isInitialized)
                {
                    if (!Slave)
                    {
                        candidate = Clamp(value, Parent.GetLeftBoundary(Id, RangeEdge.Left, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Left, AttachedHandleNo));
                    }
                    else if (Parent.AllowOverlapping)
                    {
                        candidate = Clamp(value, Min, Max);
                    }
                }
                if (_leftValue != candidate)
                {
                    ChangeLeftValue(candidate, value);
                }
            }
        }

        private void ChangeLeftValue(double value, double previousValue)
        {
            if (_isInitialized && Parent.OnEdgeMoving is not null &&
                !Parent.OnEdgeMoving.Invoke((range: this, edge: RangeEdge.Left, value: value)))
            {
                return;
            }
            _leftValue = value;

            if (previousValue != CurrentValue.Item1)
            {
                CurrentValue = (_leftValue, RightValue);
                RaiseOnChangeCallback();
            }
            if (_isInitialized && Parent.OnEdgeMoved.HasDelegate)
            {
                Parent.OnEdgeMoved.InvokeAsync((range: this, edge: RangeEdge.Left, value: value));
            }
            SetPositions();
        }

        private double _rightValue = double.MaxValue;

        private void RaiseOnChangeCallback()
        {
            if (_isInitialized)
            {
                if (OnChange.HasDelegate)
                {
                    OnChange.InvokeAsync(CurrentValue);
                }
                if (Parent.OnChange.HasDelegate)
                {
                    Parent.OnChange.InvokeAsync(CurrentValue);
                }
                if (_isDataSet && Data.OnChange.HasDelegate)
                {
                    Data.OnChange.InvokeAsync(CurrentValue);
                }
            }
        }

        // the default non-range value
        internal double RightValue
        {
            get => _rightValue;
            set
            {
                double candidate = value;
                if (_isInitialized)
                {
                    if (!Slave)
                    {
                        candidate = Clamp(value, Parent.GetLeftBoundary(Id, RangeEdge.Right, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Right, AttachedHandleNo));
                    }
                    else if (Parent.AllowOverlapping)
                    {
                        candidate = Clamp(value, Min, Max);
                    }
                }
                if (_rightValue != candidate)
                {
                    ChangeRightValue(candidate, value);
                }
            }
        }

        private void ChangeRightValue(double value, double previousValue)
        {
            if (_isInitialized && Parent.OnEdgeMoving is not null &&
                !Parent.OnEdgeMoving.Invoke((range: this, edge: RangeEdge.Right, value: value)))
            {
                return;
            }
            _rightValue = value;

            if (previousValue != CurrentValue.Item2)
            {
                CurrentValue = (LeftValue, _rightValue);
                RaiseOnChangeCallback();
            }
            if (_isInitialized && Parent.OnEdgeMoved.HasDelegate)
            {
                Parent.OnEdgeMoved.InvokeAsync((range: this, edge: RangeEdge.Right, value: value));
            }
            SetPositions();
        }

        private double Clamp(
            double value, double inclusiveMinimum, double inclusiveMaximum)
        {
            if (value < inclusiveMinimum)
            {
                value = inclusiveMinimum;
            }
            if (value > inclusiveMaximum)
            {
                value = inclusiveMaximum;
            }
            return Parent.GetNearestStep(value);
        }

        /// <summary>
        /// Fire when changes are done (onmouseup and onkeyup).
        /// </summary>
        [Parameter]
        public EventCallback<(double, double)> OnAfterChange { get; set; }

        /// <summary>
        /// Callback function that is fired when the user changes one of the values.
        /// </summary>
        [Parameter]
        public EventCallback<(double, double)> OnChange { get; set; }

        bool _tooltipPlacementSet;
        [Parameter]
        public Placement TooltipPlacement
        {
            get => _tooltipPlacement;
            set
            {
                _tooltipPlacementSet = true;
                _tooltipPlacement = value;
            }
        }

        /// <summary>
        /// If true, Tooltip will show always, or it will not show anyway, even if dragging or hovering.
        /// </summary>
        private bool _tooltipVisible;
        private bool _tooltipVisibleSet;

        private bool _tooltipRightVisible;
        private bool _tooltipLeftVisible;

        [Parameter]
        public bool TooltipVisible
        {
            get { return _tooltipVisible; }
            set
            {
                _tooltipVisibleSet = true;
                if (_tooltipVisible != value)
                {
                    _tooltipVisible = value;
                    //ensure parameter loading is not happening because values are changing during mouse moving
                    //otherwise the tooltip will be vanishing when mouse moves out of the edge
                    if (!_mouseDown)
                    {
                        _tooltipRightVisible = _tooltipVisible;
                        _tooltipLeftVisible = _tooltipVisible;
                    }
                }
            }
        }

        [Parameter]
        public string Description { get; set; }

        /// <summary>
        /// Set the range's icon 
        /// </summary>
        [Parameter]
        public string Icon { get; set; }

        private OneOf<Color, string> _fontColor;
        private string _fontColorAsString = "";
        [Parameter]
        public OneOf<Color, string> FontColor
        {
            get => _fontColor;
            set
            {
                if (!_fontColor.Equals(value))
                {
                    _customStyleChange = true;
                    _fontColorAsString = GetColorStyle(value, "color");
                    _fontColor = value;
                }
            }
        }

        private string _colorAsString = "";
        private OneOf<Color, string> _color;
        [Parameter]
        public OneOf<Color, string> Color
        {
            get => _color;
            set
            {
                if (!_color.Equals(value))
                {
                    _customStyleChange = true;
                    _colorAsString = GetColorStyle(value, "background-color");
                    _color = value;
                }
            }
        }

        OneOf<Color, string> _focusColor;
        string _focusColorAsString = "";

        [Parameter]
        public OneOf<Color, string> FocusColor
        {
            get => _focusColor;
            set
            {
                if (!_focusColor.Equals(value))
                {
                    _customStyleChange = true;
                    _focusColorAsString = GetColorStyle(value, "background-color");
                    _focusColor = value;
                }
            }
        }

        //TODO: remove when two-tone color alghoritm is going to be applied
        OneOf<Color, string> _focusBorderColor;
        private string _focusBorderColorAsString = "";
        [Parameter]
        public OneOf<Color, string> FocusBorderColor
        {
            get => _focusBorderColor;
            set
            {
                if (!_focusBorderColor.Equals(value))
                {
                    _customStyleChange = true;
                    _focusBorderColorAsString = GetColorStyle(value, "border-color");
                    _focusBorderColor = value;
                }
            }
        }
        #endregion Parameters

        private string GetColorStyle(OneOf<Color, string> color, string colorProperty)
        {
            return color.Match<string>(
                colorValue => $"{colorProperty}: {ColorHelper.GetColor(colorValue)};",
                stringValue => $"{colorProperty}: {stringValue};"
                );
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Parent.AddRangeItem(this);
            SetPositions();
            SetHasTooltipFromParent();
            SetTooltipPacementFromParent();
            SetTooltipVisibleFromParent();
        }

        internal void SetHasTooltipFromParent()
        {
            if (!_hasToolTipSet)
            {
                _hasToolTip = Parent.HasTooltip;
            }
        }

        internal void SetTooltipPacementFromParent()
        {
            if (!_tooltipPlacementSet)
            {
                _tooltipPlacement = Parent.TooltipPlacement;
            }
        }

        internal void SetTooltipVisibleFromParent()
        {
            if (!_tooltipVisibleSet)
            {
                _tooltipVisible = Parent.TooltipVisible;
                if (!_mouseDown)
                {
                    _tooltipRightVisible = _tooltipVisible;
                    _tooltipLeftVisible = _tooltipVisible;
                    if (_toolTipRight != null)
                    {
                        if (_tooltipVisible)
                        {
                            InvokeAsync(async () => await _toolTipLeft.Show());
                            InvokeAsync(async () => await _toolTipRight.Show());
                        }
                        else
                        {
                            InvokeAsync(async () => await _toolTipLeft.Hide());
                            InvokeAsync(async () => await _toolTipRight.Hide());
                        }
                    }
                }
            }
        }


        private bool _shouldRender = true;
        protected override bool ShouldRender()
        {
            if (!_shouldRender)
            {
                _shouldRender = true;
                return false;
            }
            return base.ShouldRender();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            var dict = parameters.ToDictionary();
            if (!_isInitialized)
            {
                MultiRangeSlider parent;
                if (!dict.ContainsKey(nameof(Parent)))
                {
                    throw new ArgumentNullException($"{nameof(RangeItem)} cannot be used independently. It has to be nested inside {nameof(MultiRangeSlider)}.");
                }
                else parameters.TryGetValue(nameof(Parent), out parent);
                {
                    Parent = parent;
                }

                if (dict.ContainsKey(nameof(Data)))
                {
                    Data = parameters.GetValueOrDefault<IRangeItemData>(nameof(Data), default);
                    _isDataSet = true;
                    ApplyData(true);
                }
                if (!dict.ContainsKey(nameof(Value)))
                {
                    (double, double) defaultValue = parameters.GetValueOrDefault(nameof(DefaultValue), (0d, 0d));
                    LeftValue = defaultValue.Item1;
                    RightValue = defaultValue.Item2;
                }
                else
                {
                    LeftValue = CurrentValue.Item1;
                    RightValue = CurrentValue.Item2;
                }

            }

            base.SetParametersAsync(parameters);

            if (!_isInitialized)
            {
                if (!dict.ContainsKey(nameof(TooltipPlacement)))
                {
                    if (Parent.Vertical && !_tooltipPlacementSet)
                        _tooltipPlacement = Placement.Right;
                    else
                        _tooltipPlacement = Placement.Top;
                }
            }

            _isInitialized = true;
            return Task.CompletedTask;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            ClassMapper.Clear()
                .Add($"{PreFixCls}-item")
                .If($"{PreFixCls}-disabled", () => Disabled || _parent.Disabled)
                .If($"{PreFixCls}-vertical", () => Parent.Vertical)
                //.If($"{PreFixCls}-with-marks", () => Parent.Marks != null)
                .If($"{PreFixCls}-rtl", () => RTL);

            if (_isInitialized)
            {
                ApplyData();
            }
            SetCustomStyle();
        }

        private void ApplyData(bool force = false)
        {
            if (!_isDataSet)
            {
                return;
            }
            if (force || Data.Description != Description)
            {
                Description = Data.Description;
            }
            if (force || Data.Icon != Icon)
            {
                Icon = Data.Icon;
            }
            if (force || Data.FontColor.Equals(FontColor))
            {
                FontColor = Data.FontColor;
            }
            if (force || Data.FocusColor.Equals(FocusColor))
            {
                FocusColor = Data.FocusColor;
            }
            if (force || Data.FocusBorderColor.Equals(FocusBorderColor))
            {
                FocusBorderColor = Data.FocusBorderColor;
            }
            if (force || Data.Color.Equals(Color))
            {
                Color = Data.Color;
            }
        }

        private void SetCustomStyle()
        {
            if (_customStyleChange)
            {
                if (string.IsNullOrWhiteSpace(FontColor.Value.ToString()))
                {
                    _customDescriptionStyle = "";
                }
                else
                {
                    _customDescriptionStyle = _fontColorAsString;
                }
                if (string.IsNullOrWhiteSpace(Color.Value.ToString()))
                {
                    _customTrackStyle = "";
                    _customEdgeBorderStyle = "";
                }
                else
                {
                    _customTrackStyle = _colorAsString;
                    _customEdgeBorderStyle = GetColorStyle(_color, "border-color");
                }
                if (!string.IsNullOrWhiteSpace(FocusColor.Value.ToString()) || !string.IsNullOrWhiteSpace(FocusBorderColor.Value.ToString()))
                {
                    _customFocusStyle = _focusBorderColorAsString + _focusColorAsString;
                }
                else
                {
                    _customFocusStyle = "";
                }
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                DomEventListener.AddShared<JsonElement>("window", "beforeunload", Reloading);
                DomEventListener.AddShared<JsonElement>("window", "mousemove", OnMouseMove);
                DomEventListener.AddShared<JsonElement>("window", "mouseup", OnMouseUp);
            }
            base.OnAfterRender(firstRender);
        }

        /// <summary>
        /// Indicates that a page is being refreshed
        /// </summary>
        private bool _isReloading;
        private void Reloading(JsonElement jsonElement) => _isReloading = true;

        protected override void Dispose(bool disposing)
        {
            DomEventListener.Dispose();
            Parent.RemoveRangeItem(this);
            base.Dispose(disposing);
        }

        private async Task OnKeyDown(KeyboardEventArgs e, RangeEdge handle)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var key = e.Key.ToUpperInvariant();
            double modifier = 0;
            if (Parent.Vertical)
            {
                if (key == "ARROWUP")
                {
                    modifier = 1;
                }
                if (key == "ARROWDOWN")
                {
                    modifier = -1;
                }
            }
            else
            {
                if (key == "ARROWLEFT")
                {
                    modifier = -1;
                }
                if (key == "ARROWRIGHT")
                {
                    modifier = 1;
                }
            }
            if (modifier != 0)
            {
                _valueCache = _value;
                if (Parent.Step is not null)
                {
                    if (handle == default)
                    {
                        double newLeft = LeftValue + (Parent.Step.Value * modifier);
                        double newRight = RightValue + (Parent.Step.Value * modifier);
                        await KeyMoveByValues(newLeft, newRight);
                    }
                    else
                    {
                        double oldValue = handle == RangeEdge.Left ? LeftValue : RightValue;
                        await KeyMoveByValue(handle, oldValue + (Parent.Step.Value * modifier));
                    }
                }
                else
                {
                    if (handle == default)
                    {
                        if (!(LeftValue == Min && modifier < 0) && !(RightValue == Max && modifier > 0))
                        {
                            double newLeft = Parent.Marks.Select(m => Math.Abs(m.Key + modifier * LeftValue)).Skip(1).First();
                            double newRight = Parent.Marks.Select(m => Math.Abs(m.Key + modifier * RightValue)).Skip(1).First();
                            await KeyMoveByValues(newLeft, newRight);
                        }
                    }
                    else
                    {
                        double oldValue = handle == RangeEdge.Left ? LeftValue : RightValue;
                        double newValue = Parent.Marks.Select(m => Math.Abs(m.Key + modifier * oldValue)).Skip(1).First();
                        await KeyMoveByValue(handle, newValue);
                    }
                }
            }
        }

        private Task OnKeyUp(KeyboardEventArgs e)
        {
            if (OnAfterChange.HasDelegate || Parent.OnAfterChange.HasDelegate || (_isDataSet && Data.OnAfterChange.HasDelegate))
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                var key = e.Key.ToUpperInvariant();
                bool raiseEvent = false;
                if (Parent.Vertical)
                {
                    raiseEvent = key == "ARROWUP" || key == "ARROWDOWN";
                }
                else if (!Parent.Vertical)
                {
                    raiseEvent = key == "ARROWLEFT" || key == "ARROWRIGHT";
                }
#pragma warning disable CS4014 // Does not return anything, fire & forget
                RaiseOnAfterChangeCallback(() => raiseEvent && _valueCache != _value);
#pragma warning restore CS4014 // Does not return anything, fire & forget
            }
            return Task.CompletedTask;

        }

        private async Task KeyMoveByValues(double newLeft, double newRight)
        {
            double rightCandidate = Clamp(newRight, Parent.GetLeftBoundary(Id, RangeEdge.Right, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Right, AttachedHandleNo));
            double leftCandidate = Clamp(newLeft, Parent.GetLeftBoundary(Id, RangeEdge.Left, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Left, AttachedHandleNo));

            if (leftCandidate == newLeft && rightCandidate == newRight)
            {
                ChangeLeftValue(leftCandidate, LeftValue);
                ChangeRightValue(rightCandidate, RightValue);
                var tooltipLeft = _toolTipLeft.Show();
                var tooltipRight = _toolTipRight.Show();
                await Task.WhenAll(tooltipLeft, tooltipRight);
            }
        }

        private async Task KeyMoveByValue(RangeEdge handle, double value)
        {
            if (LeftValue == RightValue)
            {
                if (handle == RangeEdge.Left)
                {
                    await SwitchToRightHandle(value);
                }
                else
                {
                    await SwitchToLeftHandle(value);
                }
            }
            else
            {
                if (handle == RangeEdge.Left)
                {
                    LeftValue = value;
                    await _toolTipLeft.Show();
                }
                else
                {
                    RightValue = value;
                    await _toolTipRight.Show();
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (_toolTipRight != null && Parent.HasTooltip)
                {
                    _rightHandle = _toolTipRight.Ref;
                    if (_toolTipLeft != null)
                    {
                        _leftHandle = _toolTipLeft.Ref;
                    }
                }
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task<(double, double, double width, double height)> GetSliderDimensions(ElementReference reference)
        {
            _sliderDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, reference);
            return ((double)(Parent.Vertical ? _sliderDom.AbsoluteTop : _sliderDom.AbsoluteLeft),
                    (double)(Parent.Vertical ? _sliderDom.ClientHeight : _sliderDom.ClientWidth),
                    (double)_sliderDom.ClientWidth, (double)_sliderDom.ClientHeight);

        }

        internal void SetFocus(bool isFocused)
        {
            if (_isFocused == isFocused)
            {
                return;
            }
            _isFocused = isFocused;

            if (_isFocused)
            {
                _focusClass = $"{PreFixCls}-track-focus";
                _focusStyle = _customFocusStyle;
                if (!(HasAttachedEdge && AttachedHandleNo == RangeEdge.Left))
                {
                    _leftFocusZIndex = "z-index: 3;"; //just below default overlay zindex
                }
                if (!(HasAttachedEdge && AttachedHandleNo == RangeEdge.Right))
                {
                    _rightFocusZIndex = "z-index: 3;";
                }
            }
            else
            {
                _focusClass = "";
                _focusStyle = "";
                if (!(HasAttachedEdge && AttachedHandleNo == RangeEdge.Left))
                {
                    _leftFocusZIndex = "z-index: 2;";
                }
                if (!(HasAttachedEdge && AttachedHandleNo == RangeEdge.Right))
                {
                    _rightFocusZIndex = "z-index: 2;";
                }
            }
            if (!isFocused)
            {
                StateHasChanged();
            }
        }

        private void OnDoubleClick(RangeEdge handle)
        {
            //TODO: bUnit: attach overlapping edge when opposite overlapping edge already attached
            if (!HasAttachedEdge || AttachedHandleNo == GetOppositeEdge(handle))
            {
                RangeItem overlappingEdgeCandidate = handle == RangeEdge.Left ? Parent.GetLeftNeighbour(Id) : Parent.GetRightNeighbour(Id);
                if (overlappingEdgeCandidate is null && !Parent.AllowOverlapping) //will be null when there are no other items or edge is closes to either Min or Max
                {
                    ResetAttached();
                    return;
                }
                bool isAttached;
                if (IsEdgeOverlapping(handle, overlappingEdgeCandidate))
                {
                    isAttached = AttachOverlappingEdges(handle, overlappingEdgeCandidate, false);
                }
                else
                {
                    isAttached = AttachNotOverlappingEdges(handle, false);
                }
                if (_isInitialized && isAttached && Parent.OnEdgeAttached.HasDelegate)
                {
                    if (handle == RangeEdge.Left)
                    {
                        Parent.OnEdgeAttached.InvokeAsync((left: AttachedItem, right: this));
                    }
                    else
                    {
                        Parent.OnEdgeAttached.InvokeAsync((left: this, right: AttachedItem));
                    }
                }
                return;
            }
            ResetAttached();
        }

        /// <summary>
        /// Will attach 2 edges. If <see cref="MultiRangeSlider.AllowOverlapping"/> is set to false,
        /// will only allow attaching neighboring edges.
        /// </summary>
        /// <param name="currentRangeEdge">Which edge of the current <see cref="RangeItem "/> needs to be attached.</param>
        /// <param name="attachToRange">RangeItem that will be attached</param>
        /// <param name="attachToRangeEdge">Which edge of the requested <see cref="RangeItem "/> needs to be attached.</param>
        /// <param name="detachExisting">Whether to detach if already attached</param>
        /// <returns>Whether attaching was successful. Returns false if attachment already exists.</returns>
        public bool AttachEdges(RangeEdge currentRangeEdge, RangeItem attachToRange, RangeEdge attachToRangeEdge, bool detachExisting = false)
        {
            if (Parent.HasAttachedEdges)
            {
                if (!detachExisting)
                {
                    return false;
                }
                else if (AttachedItem is not null && AttachedItem.Id == attachToRange.Id && AttachedHandleNo == currentRangeEdge && AttachedItem.AttachedHandleNo == attachToRangeEdge)
                {
                    return true; //are already attached
                }
                ResetAttached(true);
            }
            var currentRangeAttachResult = AttachFirstNotOverlappingEdge(currentRangeEdge, true);
            if (!currentRangeAttachResult)
            {
                return false;
            }
            attachToRange.AttachNotOverlappingEdges(attachToRangeEdge, true);

            return attachToRange.HasAttachedEdge;
        }

        /// <summary>
        /// Will initiate attaching. Same as double clicking on an edge (that is not overlapping
        /// with another edge).
        /// </summary>
        /// <param name="currentRangeEdge">Which edge of the current <see cref="RangeItem "/> needs to be attached.</param>
        /// <param name="detachExisting">Whether to detach if already attached</param>
        /// <returns>Whether attaching was successful.</returns>
        public bool AttachSingle(RangeEdge currentRangeEdge, bool detachExisting = false)
        {
            if (Parent.HasAttachedEdges)
            {
                if (!detachExisting)
                {
                    return false;
                }
                ResetAttached(true);
            }

            return AttachNotOverlappingEdges(currentRangeEdge, true, false);
        }

        /// <summary>
        /// Will attach overlapping edges. Same as double clicking on overlapping edges.
        /// </summary>
        /// <param name="currentRangeEdge">Which edge of the current <see cref="RangeItem "/> needs to be attached.</param>
        /// <param name="detachExisting">Whether to detach if already attached</param>
        /// <returns>Whether attaching was successful. Returns true if already attached.</returns>
        public bool AttachOverlappingEdges(RangeEdge currentRangeEdge, bool detachExisting = false)
        {
            if (Parent.HasAttachedEdges)
            {
                if (!detachExisting)
                {
                    return false;
                }
            }

            RangeItem overlappingEdgeCandidate = currentRangeEdge == RangeEdge.Left ? Parent.GetLeftNeighbour(Id) : Parent.GetRightNeighbour(Id);
            if (overlappingEdgeCandidate is null) //will be null when there are no other items or edge is closes to either Min or Max
            {
                return false;
            }
            if (Parent.HasAttachedEdges &&
                (
                    Parent.ItemRequestingAttach.Id == overlappingEdgeCandidate.Id
                    ||
                    Parent.ItemRespondingToAttach.Id == overlappingEdgeCandidate.Id
                ))
            {
                return true; //is already attached
            }
            else
            {
                ResetAttached(true);
            }

            if (IsEdgeOverlapping(currentRangeEdge, overlappingEdgeCandidate))
            {
                AttachOverlappingEdges(currentRangeEdge, overlappingEdgeCandidate, true);
            }
            return HasAttachedEdge;
        }

        /// <summary>
        /// Detaches edge.
        /// </summary>        
        /// <returns>Whether detachment was successful. Returns true if no attachment existed.</returns>
        public bool DetachEdges()
        {
            if (Parent.HasAttachedEdges)
            {
                ResetAttached();
                return true;
            }
            return false;
        }

        public RangeEdge GetAttachedEdge() => AttachedHandleNo;
        internal bool AttachNotOverlappingEdges(RangeEdge handle, bool outsideCall, bool detachExisting = true)
        {
            if (Parent.ItemRequestingAttach is null)
            {
                return AttachFirstNotOverlappingEdge(handle, outsideCall);
            }
            if (Parent.AllowOverlapping)
            {
                if (Parent.ItemRespondingToAttach is null && Parent.ItemRequestingAttach.Id != Id) //do not allow same item edge locks, use dragging
                {
                    return AttachSecondNotOverlappingEdge(handle, outsideCall,
                        handle == Parent.ItemRequestingAttach.HandleNoRequestingAttaching);
                }
                else if (detachExisting)
                {
                    ResetAttached(true);
                    return false;
                }
            }
            else
            {
                if (AreEdgesNeighbours(handle))
                {
                    return AttachSecondNotOverlappingEdge(handle, outsideCall);
                }
                else if (detachExisting)
                {
                    ResetAttached(true);
                    return false;
                }
            }
            return true;
        }

        private bool ShouldCancelAttaching(bool outsideCall, RangeEdge handle, RangeItem currentItem, RangeItem attachedItem)
        {
            if (_isInitialized && !outsideCall && Parent.OnEdgeAttaching is not null)
            {
                bool allowAttaching;
                bool detachExistingOnCancel;
                if (handle == RangeEdge.Left)
                {
                    (allowAttaching, detachExistingOnCancel) = Parent.OnEdgeAttaching((left: currentItem, right: attachedItem));
                }
                else
                {
                    (allowAttaching, detachExistingOnCancel) = Parent.OnEdgeAttaching((left: attachedItem, right: currentItem));
                }
                if (!allowAttaching)
                {
                    if (detachExistingOnCancel)
                    {
                        ResetAttached(true);
                    }
                    return true;
                }
            }
            return false;

        }

        private bool AttachFirstNotOverlappingEdge(RangeEdge handle, bool outsideCall)
        {
            if (ShouldCancelAttaching(outsideCall, handle, this, null))
            {
                return false;
            }

            Parent.ItemRequestingAttach = this;
            HandleNoRequestingAttaching = handle;
            SetLockEdgeStyle(handle);
            return true;
        }

        private bool AttachSecondNotOverlappingEdge(RangeEdge handle, bool outsideCall, bool isSameHandle = false)
        {
            if (ShouldCancelAttaching(outsideCall, handle, Parent.ItemRequestingAttach, this)) //reversed order is intentional
            {
                return false;
            }

            Parent.ItemRespondingToAttach = this;
            Master = true;
            Parent.ItemRequestingAttach.Slave = true;
            HasAttachedEdgeWithGap = true;
            Parent.ItemRequestingAttach.HasAttachedEdgeWithGap = true;
            Parent.ItemRequestingAttach.AttachedHandleNo = Parent.ItemRequestingAttach.HandleNoRequestingAttaching;
            AttachedHandleNo = handle;
            AttachedItem = Parent.ItemRequestingAttach;
            Parent.ItemRequestingAttach.AttachedItem = this;
            if (!isSameHandle)
            {
                if (handle == RangeEdge.Left)
                {
                    GapDistance = this.LeftValue - AttachedItem.RightValue;
                    ChangeAttachedItem = () => AttachedItem.RightValue = this.LeftValue - GapDistance;
                    AttachedItem.ChangeAttachedItem = () => this.LeftValue = AttachedItem.RightValue + GapDistance;
                }
                else
                {
                    GapDistance = AttachedItem.LeftValue - this.RightValue;
                    ChangeAttachedItem = () => AttachedItem.LeftValue = this.RightValue + GapDistance;
                    AttachedItem.ChangeAttachedItem = () => this.RightValue = AttachedItem.LeftValue - GapDistance;
                }
                Parent.ItemRequestingAttach.SetLockEdgeStyle(GetOppositeEdge(handle), true, true);
            }
            else
            {
                if (handle == RangeEdge.Left)
                {
                    GapDistance = this.LeftValue - AttachedItem.LeftValue;
                    ChangeAttachedItem = () => AttachedItem.LeftValue = this.LeftValue - GapDistance;
                    AttachedItem.ChangeAttachedItem = () => this.LeftValue = AttachedItem.LeftValue + GapDistance;
                }
                else
                {
                    GapDistance = AttachedItem.RightValue - this.RightValue;
                    ChangeAttachedItem = () => AttachedItem.RightValue = this.RightValue + GapDistance;
                    AttachedItem.ChangeAttachedItem = () => this.RightValue = AttachedItem.RightValue - GapDistance;
                }
                Parent.ItemRequestingAttach.SetLockEdgeStyle(handle, true, true);
            }
            AttachedItem.GapDistance = GapDistance;
            SetLockEdgeStyle(handle, true);
            return true;
        }

        private double CalculateGapDistance()
        {
            if (AttachedHandleNo != AttachedItem.AttachedHandleNo)
            {
                if (AttachedHandleNo == RangeEdge.Left)
                {
                    return this.LeftValue - AttachedItem.RightValue;
                }
                return AttachedItem.LeftValue - this.RightValue;
            }
            if (AttachedHandleNo == RangeEdge.Left)
            {
                return this.LeftValue - AttachedItem.LeftValue;
            }
            return AttachedItem.RightValue - this.RightValue;
        }

        private bool AreEdgesNeighbours(RangeEdge handle)
        {
            if (Parent.ItemRespondingToAttach is not null)
            {
                //nothing to attach, since is already attached
                return false;
            }
            if (Parent.ItemRequestingAttach.HandleNoRequestingAttaching == handle)
            {
                //if same edge, then not a neighbor
                return false;
            }

            return IsLeftNeighbor(handle)
                || IsRightNeighbor(handle);
        }

        private bool IsRightNeighbor(RangeEdge handle)
        {
            return handle == RangeEdge.Right
                && Parent.ItemRequestingAttach.Id == Parent.GetRightNeighbour(Id).Id;
        }

        private bool IsLeftNeighbor(RangeEdge handle)
        {
            return handle == RangeEdge.Left
                && Parent.ItemRequestingAttach.Id == Parent.GetLeftNeighbour(Id).Id;
        }

        /// <summary>
        /// Evaluates overlapping edges (neighboring)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="overlappingEdgeCandidate"></param>
        /// <returns></returns>
        private bool IsEdgeOverlapping(RangeEdge handle, RangeItem overlappingEdgeCandidate)
        {
            if (overlappingEdgeCandidate is null)
            {
                return false;
            }

            return IsOverlappingWithLeftEdge(handle, overlappingEdgeCandidate)
                || IsOverlappingWithRightEdge(handle, overlappingEdgeCandidate);

        }

        private bool IsOverlappingWithLeftEdge(RangeEdge handle, RangeItem overlappingEdgeCandidate)
        {
            return handle == RangeEdge.Left && overlappingEdgeCandidate.RightValue == LeftValue;
        }

        private bool IsOverlappingWithRightEdge(RangeEdge handle, RangeItem overlappingEdgeCandidate)
        {
            return handle == RangeEdge.Right && overlappingEdgeCandidate.LeftValue == RightValue;
        }

        private bool AttachOverlappingEdges(RangeEdge handle, RangeItem item, bool outsideCall)
        {
            if (ShouldCancelAttaching(outsideCall, handle, this, item))
            {
                return false;
            }
            if (Parent.HasAttachedEdges)
            {
                if (!ResetAttached(true))
                {
                    return false;
                }
            }

            AttachedItem = item;
            HasAttachedEdge = true;
            AttachedHandleNo = handle;
            Master = true;

            AttachedItem.Slave = true;
            AttachedItem.HasAttachedEdge = true;
            AttachedItem.AttachedItem = this;
            AttachedItem.AttachedHandleNo = GetOppositeEdge(handle);

            Parent.ItemRequestingAttach = this;
            Parent.ItemRespondingToAttach = AttachedItem;

            if (handle == RangeEdge.Left)
            {
                ChangeAttachedItem = () => AttachedItem.RightValue = this.LeftValue;
                AttachedItem.ChangeAttachedItem = () => this.LeftValue = AttachedItem.RightValue;
            }
            else
            {
                ChangeAttachedItem = () => AttachedItem.LeftValue = this.RightValue;
                AttachedItem.ChangeAttachedItem = () => this.RightValue = AttachedItem.LeftValue;
            }
            SetLockEdgeStyle(handle, true);
            return true;
        }

        private static RangeEdge GetOppositeEdge(RangeEdge edge)
        {
            if (edge == RangeEdge.Left)
            {
                return RangeEdge.Right;
            }
            return RangeEdge.Left;
        }

        private void SetLockEdgeStyle(RangeEdge handle, bool locked = false, bool requestStateChange = false)
        {
            if (handle == RangeEdge.Left)
            {
                ApplyLockEdgeStyle(locked, ref _attachedLeftHandleClass, ref _leftHandleFill, ref _leftFocusZIndex);
            }
            else
            {
                ApplyLockEdgeStyle(locked, ref _attachedRightHandleClass, ref _rightHandleFill, ref _rightFocusZIndex);
            }
            _shouldRender = true;
            if (requestStateChange)
            {
                StateHasChanged();
            }
        }

        private static void ApplyLockEdgeStyle(bool locked, ref string attachHandleClass, ref RenderFragment handleFill, ref string focusIndex)
        {
            if (locked)
            {
                attachHandleClass = $" {PreFixCls}-handle-lock {PreFixCls}-handle-lock-closed";
                handleFill = _locked;
            }
            else
            {
                attachHandleClass = $" {PreFixCls}-handle-lock {PreFixCls}-handle-lock-open";
                handleFill = _unlocked;
            }
            focusIndex = "z-index: 4;";
        }

        internal void ResetLockEdgeStyle(bool requestStateChange)
        {
            _shouldRender = true;
            _attachedLeftHandleClass = "";
            _attachedRightHandleClass = "";
            _leftHandleFill = null;
            _rightHandleFill = null;
            _leftFocusZIndex = "z-index: 2;";
            _rightFocusZIndex = "z-index: 2;";
            if (requestStateChange)
            {
                StateHasChanged();
            }
        }

        private bool ResetAttached(bool forceReset = false)
        {
            if (!Parent.HasAttachedEdges && !HasAttachedEdge && !forceReset)
            {
                return true; //nothing to detach, don't fail
            }
            RangeItem left = null, right = null;
            if (Parent.OnEdgeDetaching is not null || Parent.OnEdgeDetached.HasDelegate)
            {
                Parent.GetAttachedInOrder(out left, out right);
                if (Parent.OnEdgeDetaching is not null && !Parent.OnEdgeDetaching.Invoke((left, right)))
                {
                    return false;
                }
            }

            bool requestStateChange = Parent.HasAttachedEdges && !HasAttachedEdge;

            //reset all attached
            if (HasAttachedEdgeWithGap || forceReset)
            {
                ResetNotOverlapping(Parent.ItemRequestingAttach, Id);
                Parent.ItemRequestingAttach = null;
                ResetNotOverlapping(Parent.ItemRespondingToAttach, Id);
                Parent.ItemRespondingToAttach = null;
                //re-sort, because order could have been altered
                //and proper order is needed to pick-up attaching on 
                //overlapping edges
                Parent.SortRangeItems();
            }
            else
            {
                if (AttachedItem is not null)
                {
                    AttachedItem.Slave = false;
                }
                ResetLockEdgeStyle(false);
                Parent.ItemRequestingAttach = null;
                Parent.ItemRespondingToAttach = null;

                Master = false;
                AttachedItem.Slave = false;
                AttachedItem.HasAttachedEdge = false;
                AttachedItem.AttachedItem = null;
                AttachedItem.AttachedHandleNo = 0;
                AttachedItem.ChangeAttachedItem = default;
                AttachedItem.ResetLockEdgeStyle(false);

                AttachedItem = default;
                AttachedHandleNo = 0;

                ChangeAttachedItem = default;
            }
            SetFocus(_isFocused);
            HasAttachedEdgeWithGap = false;
            if (Parent.OnEdgeDetached.HasDelegate)
            {
                Parent.OnEdgeDetached.InvokeAsync((left, right));
            }
            return true;
        }

        private static void ResetNotOverlapping(RangeItem item, string currentRangeId)
        {
            if (item is not null)
            {
                item.ResetLockEdgeStyle(item.Id != currentRangeId);
                item.ChangeAttachedItem = default;
                item.HandleNoRequestingAttaching = 0;
                item.AttachedHandleNo = 0;
                item.AttachedItem = null;
                item.HasAttachedEdgeWithGap = false;
                item.GapDistance = 0;
                item.Master = false;
                item.Slave = false;
                item.AttachedItem = null;
            }
        }

        private async Task OnRangeItemClick(MouseEventArgs args)
        {
            if (!_isFocused)
            {
                SetFocus(true);
                Parent.SetRangeItemFocus(this, true);
            }
            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
            _trackedClientX = args.ClientX;
            _trackedClientY = args.ClientY;
            if (_toolTipRight != null)
            {
                _tooltipRightVisible = true;
                _tooltipLeftVisible = true;
                var tooltipRight = _toolTipRight.Show();
                var tooltipLeft = _toolTipLeft.Show();
                await Task.WhenAll(tooltipLeft, tooltipRight);
            }

            //evaluate clicked position in respect to each edge
            _mouseDownOnTrack = !Disabled && !Parent.Disabled;
            (double sliderOffset, double sliderLength, double sliderWidth, double sliderHeight)
                = await GetSliderDimensions(Parent._railRef);
            _trackedClientWidth = sliderWidth;
            sliderHeight = _trackedClientHeight;
            double clickedValue = CalculateNewHandleValue(Parent.Vertical ? args.PageY : args.PageX, sliderOffset, sliderLength);
            _distanceToLeftHandle = clickedValue - LeftValue;
            _distanceToRightHandle = RightValue - clickedValue;
            _valueCache = _value;
            if (HasAttachedEdge && !Master)
            {
                SetAsMaster();
            }
        }

        private double _trackedClientX;
        private double _trackedClientY;
        private double _trackedClientWidth;
        private double _trackedClientHeight;

        private void OnMouseDownEdge(MouseEventArgs args, RangeEdge edge)
        {
            _mouseDown = !Disabled && !Parent.Disabled;
            SetFocus(true);
            Parent.SetRangeItemFocus(this, true);
            _right = edge == RangeEdge.Right;
            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
            _trackedClientX = args.ClientX;
            _trackedClientY = args.ClientY;
            _trackedClientWidth = _trackedClientX;
            _trackedClientHeight = _trackedClientY;
            _valueCache = _value;
            if (_toolTipRight != null)
            {
                if (_right)
                {
                    _tooltipRightVisible = true;
                }
                else
                {
                    _tooltipLeftVisible = true;
                }
            }
            if (HasAttachedEdgeWithGap && !Master)
            {
                SetAsMaster();
            }
        }

        private void SetAsMaster()
        {
            Master = true;
            Slave = false;
            AttachedItem.Master = false;
            AttachedItem.Slave = true;
        }

        private bool IsMoveWithinBoundary(JsonElement jsonElement)
        {
            double clientX = jsonElement.GetProperty("clientX").GetDouble();
            double clientY = jsonElement.GetProperty("clientY").GetDouble();
            bool xCoordinateIsWithinBoundary = _trackedClientX <= clientX && clientX <= _trackedClientX + _trackedClientWidth;
            bool yCoordinateIsWithinBoundary = _trackedClientY <= clientY && clientY <= _trackedClientY + _trackedClientHeight;
            return xCoordinateIsWithinBoundary && yCoordinateIsWithinBoundary;
        }

        private async void OnMouseMove(JsonElement jsonElement)
        {
            if (_mouseDown)
            {
                await ApplyMouseMove(jsonElement, CalculateValueAsync);
            }
            if (_mouseDownOnTrack)
            {
                IsRangeDragged = true;
                await ApplyMouseMove(jsonElement, CalculateValuesAsync);
            }
        }

        private async Task ApplyMouseMove(JsonElement jsonElement, Func<double, Task<bool>> predicate)
        {
            _trackedClientX = jsonElement.GetProperty("clientX").GetDouble();
            _trackedClientY = jsonElement.GetProperty("clientY").GetDouble();
            double clickPosition = Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble();
            if (!await predicate(clickPosition))
            {
                _shouldRender = false;
            }
        }

        private async void OnMouseUp(JsonElement jsonElement)
        {
            bool isMoveInEdgeBoundary = IsMoveWithinBoundary(jsonElement);
            if (!_mouseDown && !_mouseDownOnTrack && isMoveInEdgeBoundary)
            {
                //force blazor OnMouseDown events to run first
                await AsyncHelper.WaitFor(() => _mouseDown | _mouseDownOnTrack);
            }
            _shouldRender = true;

            bool raiseOnAfterChangeEvent = _mouseDown || _mouseDownOnTrack;
            if (_mouseDown)
            {
                _mouseDown = false;
                _trackedClientHeight = double.MinValue;
                _trackedClientWidth = double.MinValue;
                if (!isMoveInEdgeBoundary)
                {
                    await CalculateValueAsync(Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble());
                }
            }
            if (_mouseDownOnTrack)
            {
                _mouseDownOnTrack = false;
                IsRangeDragged = false;
                _trackedClientHeight = double.MinValue;
                _trackedClientWidth = double.MinValue;
                if (HasAttachedEdgeWithGap)
                {
                    GapDistance = CalculateGapDistance();
                    AttachedItem.GapDistance = GapDistance;
                }
            }
#pragma warning disable CS4014 // Does not return anything, fire & forget            
            RaiseOnAfterChangeCallback(() => raiseOnAfterChangeEvent && _valueCache != _value);
#pragma warning restore CS4014 // Does not return anything, fire & forget

            if (_toolTipRight != null)
            {
                if (_tooltipRightVisible != TooltipVisible)
                {
                    _tooltipRightVisible = TooltipVisible;
                    _toolTipRight.SetVisible(TooltipVisible);
                }

                if (_tooltipLeftVisible != TooltipVisible)
                {
                    _tooltipLeftVisible = TooltipVisible;
                    _toolTipLeft.SetVisible(TooltipVisible);
                }
            }

            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
        }

        private Task RaiseOnAfterChangeCallback(Func<bool> predicate)
        {
            if (predicate.Invoke())
            {
                if (OnAfterChange.HasDelegate)
                {
                    OnAfterChange.InvokeAsync(CurrentValue);
                }
                if (Parent.OnAfterChange.HasDelegate)
                {
                    Parent.OnAfterChange.InvokeAsync(CurrentValue);
                }
                if (_isDataSet && Data.OnAfterChange.HasDelegate)
                {
                    Data.OnAfterChange.InvokeAsync(CurrentValue);
                }
            }
            return Task.CompletedTask;
        }

        private async Task<bool> CalculateValueAsync(double clickClient)
        {
            (double sliderOffset, double sliderLength, _, _) = await GetSliderDimensions(Parent._railRef);
            bool hasChanged;
            if (_right)
            {
                double rightV = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
                hasChanged = await HasValueChanged(ref _rightValue, () => ProcessNewRightValue(rightV));
            }
            else
            {
                double leftV = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
                hasChanged = await HasValueChanged(ref _leftValue, () => ProcessNewLeftValue(leftV));
            }
            if (hasChanged)
            {
                ChangeAttachedItem?.Invoke();
            }
            return hasChanged;
        }

        private async Task<bool> CalculateValuesAsync(double clickClient)
        {
            (double sliderOffset, double sliderLength, _, _) = await GetSliderDimensions(Parent._railRef);

            double dragPosition = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
            double rightV = dragPosition + _distanceToRightHandle;
            double leftV = dragPosition - _distanceToLeftHandle;
            if (rightV - leftV != RightValue - LeftValue)
            {
                //movement is shrinking the range, abort
                return false;
            }
            if (HasAttachedEdge)
            {
                return await CalculateValuesWithAttachedEdgesAsync(rightV, leftV);
            }
            else
            {
                //evaluate if both rightV & leftV are within acceptable values
                double rightCandidate = Clamp(rightV, Parent.GetLeftBoundary(Id, RangeEdge.Right, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Right, AttachedHandleNo));
                double leftCandidate = Clamp(leftV, Parent.GetLeftBoundary(Id, RangeEdge.Left, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Left, AttachedHandleNo));
                if (leftCandidate != LeftValue && rightCandidate != RightValue)
                {
                    ChangeLeftValue(leftCandidate, leftV);
                    ChangeRightValue(rightCandidate, rightV);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CalculateValuesWithAttachedEdgesAsync(double rightV, double leftV)
        {
            bool hasChanged = false;
            if (AttachedHandleNo == RangeEdge.Left)
            {
                hasChanged = await HasValueChanged(ref _leftValue, () => ProcessNewLeftValue(leftV));
            }
            else
            {
                hasChanged = await HasValueChanged(ref _rightValue, () => ProcessNewRightValue(rightV));
            }

            if (hasChanged)
            {
                if (AttachedHandleNo == RangeEdge.Left)
                {
                    await ProcessNewRightValue(rightV);
                }
                else
                {
                    await ProcessNewLeftValue(leftV);
                }
                ChangeAttachedItem?.Invoke();
            }
            return true;
        }

        private Task<bool> HasValueChanged(ref double value, Func<Task> predicate)
        {
            double valueB4Change = value;
            predicate.Invoke();
            double newValue = value;
            return Task.FromResult(valueB4Change != newValue);
        }

        private async Task ProcessNewRightValue(double rightV)
        {
            if (rightV < LeftValue)
            {
                if (Parent.AllowOverlapping && HasAttachedEdge) //push
                {
                    RightValue = rightV;
                    LeftValue = rightV;
                }
                else if (!HasAttachedEdge) //do not allow switching if locked with another range item
                {
                    await SwitchToLeftHandle(rightV);
                }
                else
                {
                    return;
                }
            }
            else
            {
                RightValue = rightV;
            }
        }

        private async Task SwitchToLeftHandle(double rightV)
        {
            _right = false;
            if (_mouseDown)
                RightValue = _initialLeftValue;
            LeftValue = rightV;
            SwitchTooltip(RangeEdge.Left);
            await FocusAsync(_leftHandle);
        }

        private async Task ProcessNewLeftValue(double leftV)
        {
            if (leftV > RightValue)
            {
                if (Parent.AllowOverlapping && HasAttachedEdge) //push
                {
                    RightValue = leftV;
                    LeftValue = leftV;
                }
                else if (!HasAttachedEdge) //do not allow switching if locked with another range item
                {
                    await SwitchToRightHandle(leftV);
                }
                else
                {
                    return;
                }
            }
            else
            {
                LeftValue = leftV;
            }
        }

        private async Task SwitchToRightHandle(double leftV)
        {
            _right = true;
            if (_mouseDown)
                LeftValue = _initialRightValue;
            RightValue = leftV;
            SwitchTooltip(RangeEdge.Right);
            await FocusAsync(_rightHandle);
        }

        private void SwitchTooltip(RangeEdge toHandle)
        {
            if (_toolTipRight == null)
            {
                return;
            }

            if (toHandle == RangeEdge.Left)
            {

                if (_tooltipRightVisible != TooltipVisible)
                {
                    _tooltipRightVisible = TooltipVisible;
                    _toolTipRight.SetVisible(TooltipVisible);
                }
                _tooltipLeftVisible = true;
                return;
            }

            if (_tooltipLeftVisible != TooltipVisible)
            {
                _tooltipLeftVisible = TooltipVisible;
                _toolTipLeft.SetVisible(TooltipVisible);
            }
            _tooltipRightVisible = true;
        }

        private double CalculateNewHandleValue(double clickClient, double sliderOffset, double sliderLength)
        {
            double handleNewPosition;
            if (Parent.Reverse)
            {
                if (Parent.Vertical)
                {
                    handleNewPosition = clickClient - sliderOffset;
                }
                else
                {
                    handleNewPosition = sliderLength - (clickClient - sliderOffset);
                }
            }
            else
            {
                if (Parent.Vertical)
                {
                    handleNewPosition = sliderOffset + sliderLength - clickClient;
                }
                else
                {
                    handleNewPosition = clickClient - sliderOffset;
                }
            }

            return (Parent.MinMaxDelta * handleNewPosition / sliderLength) + Min;
        }

        internal void SetPositions()
        {
            var rightHandPercentage = (RightValue - Min) / Parent.MinMaxDelta;
            var leftHandPercentage = (LeftValue - Min) / Parent.MinMaxDelta;
            string rightHandStyle;
            string leftHandStyle;
            string trackStart;
            string trackSize;
            double trackStartAdjust = 0;
            double trackSizeAdjust = 0;
            if (LeftValue != Min)
            {
                trackStartAdjust = Parent.ItemAdjust;
                trackSizeAdjust = Parent.ItemAdjust;
            }
            if (RightValue != Max)
            {
                trackSizeAdjust += Parent.ItemAdjust;
            }

            //TODO: consider using delegates
            if (Parent.Vertical)
            {
                rightHandStyle = MultiRangeSlider.GetVerticalCoordinate(rightHandPercentage);
                leftHandStyle = MultiRangeSlider.GetVerticalCoordinate(leftHandPercentage);
                trackStart = MultiRangeSlider.GetVerticalCoordinate(leftHandPercentage - trackStartAdjust);
                trackSize = MultiRangeSlider.GetVerticalTrackSize(leftHandPercentage - trackStartAdjust, rightHandPercentage + (trackSizeAdjust - trackStartAdjust));
            }
            else
            {
                rightHandStyle = Formatter.ToPercentWithoutBlank(rightHandPercentage);
                leftHandStyle = Formatter.ToPercentWithoutBlank(leftHandPercentage);
                trackStart = Formatter.ToPercentWithoutBlank(leftHandPercentage - trackStartAdjust);
                trackSize = Formatter.ToPercentWithoutBlank(((RightValue - LeftValue) / Parent.MinMaxDelta) + trackSizeAdjust);
            }
            _rightHandleCssPosition = string.Format(CultureInfo.CurrentCulture, RightHandleStyleFormat, rightHandStyle);
            _trackCssPosition = string.Format(CultureInfo.CurrentCulture, TrackStyleFormat, trackStart, trackSize);
            _leftHandleCssPosition = string.Format(CultureInfo.CurrentCulture, LeftHandleStyleFormat, leftHandStyle);
            _shouldRender = true;
            StateHasChanged();
        }

        protected override void OnValueChange((double, double) value)
        {
            base.OnValueChange(value);

            if (LeftValue != value.Item1)
            {
                LeftValue = value.Item1;
            }
            if (RightValue != value.Item2)
            {
                RightValue = value.Item2;
            }
        }

        private bool IsLeftAndRightChanged((double, double) value)
        {
            return (value.Item1 != LeftValue) && (value.Item2 != RightValue);
        }

        private (double, double) _value;
        private MultiRangeSlider _parent;
        private bool _hasAttachedEdgeWithGap;
        private bool _hasAttachedEdge;
        private Placement _tooltipPlacement;

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter]
        public sealed override (double, double) Value
        {
            get { return _value; }
            set
            {
                (double, double) orderedValue = SortValue(value);
                var hasChanged = orderedValue.Item1 != Value.Item1 || orderedValue.Item2 != Value.Item2;
                if (hasChanged)
                {
                    _value = orderedValue;
                    OnValueChange(orderedValue);
                }
            }
        }

        private (double, double) SortValue((double, double) value)
        {
            if (value.Item1 > value.Item2)
            {
                return (value.Item2, value.Item1);
            }
            return value;
        }
    }


}
