using System;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using AntDesign.Core.Helpers;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AntDesign
{
    public enum RangeEdge
    {
        Left = 1,
        Right = 2,
    }

    public partial class RangeItem : AntInputComponentBase<(double, double)>
    {
        private const string PreFixCls = "ant-multi-range-slider-item";
        private HtmlElement _sliderDom;
        private HtmlElement _leftHandleDom;
        private HtmlElement _rightHandleDom;
        private ElementReference _leftHandle;
        private ElementReference _rightHandle;
        private string _leftHandleStyle = "left: 0%; right: auto; transform: translateX(-50%);";
        private string _rightHandleStyle = "left: 0%; right: auto; transform: translateX(-50%);";
        private string _trackStyle = "left: 0%; width: 0%; right: auto;";
        private bool _isFocused;
        private string _focusClass = "";
        private string _leftFocusZIndex = "z-index: 900;";
        private string _rightFocusZIndex = "z-index: 900;";
        private bool _mouseDown;
        private bool _mouseMove;
        private bool _right = true;
        private bool _isInitialized = false;
        private double _initialLeftValue;
        private double _initialRightValue;
        private Tooltip _toolTipRight;
        private Tooltip _toolTipLeft;

        private bool _hasAttachedEdge;
        internal bool HasAttachedEdgeWithGap
        {
            get => _hasAttachedEdgeWithGap;
            set
            {
                _hasAttachedEdgeWithGap = value;
                _hasAttachedEdge = value;

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
                        return "bottom: auto; top: {0}; transform: translateY(-50%);";
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
                        return "bottom: auto; top: {0}; transform: translateY(-50%);";
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


        /// <summary>
        /// The default value of slider. When <see cref="Range"/> is false, use number, otherwise, use [number, number]
        /// </summary>
        [Parameter]
        public (double, double) DefaultValue { get; set; }

        /// <summary>
        /// If true, the slider will not be interactable
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Whether the thumb can drag over tick only
        /// </summary>
        [Parameter]
        public bool Dots { get; set; }

        /// <summary>
        /// The maximum value the slider can slide to
        /// </summary>

        public double Max => Parent.Max;

        /// <summary>
        /// The minimum value the slider can slide to
        /// </summary>

        public double Min => Parent.Min;

        ///// <summary>
        ///// dual thumb mode
        ///// </summary>
        ////[Parameter]
        //private bool? _range;

        //public bool Range
        //{
        //    get
        //    {
        //        if (_range == null)
        //        {
        //            Type type = typeof(TValue);
        //            Type doubleType = typeof(double);
        //            Type tupleDoubleType = typeof((double, double));
        //            if (type == doubleType)
        //            {
        //                _range = false;
        //            }
        //            else if (type == tupleDoubleType)
        //            {
        //                _range = true;
        //            }
        //            else
        //            {
        //                throw new ArgumentOutOfRangeException($"Type argument of Slider should be one of {doubleType}, {tupleDoubleType}");
        //            }
        //        }
        //        return _range.Value;
        //    }
        //    //private set { _range = value; }
        //}

        ///// <summary>
        ///// The granularity the slider can step through values. Must greater than 0, and be divided by (<see cref="Max"/> - <see cref="Min"/>) . When <see cref="Marks"/> no null, <see cref="Step"/> can be null.
        ///// </summary>
        //private double? _step = 1;

        //private int _precision;

        //[Parameter]
        //public double? Step
        //{
        //    get { return _step; }
        //    set
        //    {
        //        _step = value;
        //        //no need to evaluate if no tooltip
        //        if (_step != null && _isTipFormatterDefault)
        //        {
        //            char separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        //            string[] number = _step.ToString().Split(separator);
        //            if (number.Length > 1)
        //            {
        //                _precision = number[1].Length;
        //                _tipFormatter = (d) => string.Format(CultureInfo.CurrentCulture, "{0:N02}", Math.Round(d, _precision));
        //            }
        //        }
        //    }
        //}

        private double _leftValue = double.MinValue;

        internal double LeftValue
        {
            get => _leftValue;
            set
            {
                double candidate;
                if (!Slave)
                {
                    candidate = Clamp(value, Parent.GetLeftBoundary(Id, RangeEdge.Left, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Left, AttachedHandleNo));
                }
                else
                {
                    if (Parent.AllowOverlapping)
                    {
                        candidate = Clamp(value, Min, Max);
                    }
                    else
                    {
                        candidate = value;
                    }
                }
                if (_leftValue != candidate)
                {
                    _leftValue = candidate;
                    SetStyle();
                    if (value != CurrentValue.Item1)
                        CurrentValue = (_leftValue, RightValue);
                }
            }
        }

        private double _rightValue = double.MaxValue;

        // the default non-range value
        internal double RightValue
        {
            get => _rightValue;
            set
            {
                //Console.WriteLine($"RightValue change => clamp {value} based on LeftValue={LeftValue} and RightBoundary={Parent.GetRightBoundary(Id, _hasAttachedEdge)}");
                double candidate;
                if (!Slave)
                {
                    candidate = Clamp(value, Parent.GetLeftBoundary(Id, RangeEdge.Right, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Right, AttachedHandleNo));
                }
                else
                {
                    if (Parent.AllowOverlapping)
                    {
                        candidate = Clamp(value, Min, Max);
                    }
                    else
                    {
                        candidate = value;
                    }
                }
                if (_rightValue != candidate)
                {
                    _rightValue = candidate;
                    SetStyle();
                    //CurrentValue = TupleToGeneric((LeftValue, _rightValue));
                    //(double, double) typedValue = DataConvertionExtensions.Convert<TValue, (double, double)>(CurrentValue);
                    if (value != CurrentValue.Item2)
                        CurrentValue = (LeftValue, _rightValue);
                }
            }
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
        /// Fire when onmouseup is fired.
        /// </summary>
        [Parameter]
        public Action<(double, double)> OnAfterChange { get; set; } //use Action here intead of EventCallback, otherwise VS will not complie when user add a delegate

        /// <summary>
        /// Callback function that is fired when the user changes the slider's value.
        /// </summary>
        [Parameter]
        public Action<(double, double)> OnChange { get; set; }

        //[Parameter]
        //public bool HasTooltip { get; set; } = true;

        ///// <summary>
        ///// Slider will pass its value to tipFormatter, and display its value in Tooltip
        ///// </summary>
        //private bool _isTipFormatterDefault = true;

        //private Func<double, string> _tipFormatter = (d) => d.ToString(LocaleProvider.CurrentLocale.CurrentCulture);

        //[Parameter]
        //public Func<double, string> TipFormatter
        //{
        //    get { return _tipFormatter; }
        //    set
        //    {
        //        _tipFormatter = value;
        //        _isTipFormatterDefault = false;
        //    }
        //}

        /// <summary>
        /// Set Tooltip display position. Ref Tooltip
        /// </summary>
        [Parameter]
        public Placement TooltipPlacement { get; set; }

        /// <summary>
        /// If true, Tooltip will show always, or it will not show anyway, even if dragging or hovering.
        /// </summary>
        private bool _tooltipVisible;

        private bool _tooltipRightVisible;
        private bool _tooltipLeftVisible;

        [Parameter]
        public bool TooltipVisible
        {
            get { return _tooltipVisible; }
            set
            {
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

        /// <summary>
        /// The DOM container of the Tooltip, the default behavior is to create a div element in body.
        /// </summary>
        [Parameter]
        public object GetTooltipPopupContainer { get; set; }

        #endregion Parameters

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Parent.AddRangeItem(this);
            SetStyle();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            base.SetParametersAsync(parameters);

            var dict = parameters.ToDictionary();
            if (!_isInitialized)
            {
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
                if (!dict.ContainsKey(nameof(TooltipPlacement)))
                {
                    if (Parent.Vertical)
                        TooltipPlacement = Placement.Right;
                    else
                        TooltipPlacement = Placement.Top;
                }
            }

            _isInitialized = true;
            return Task.CompletedTask;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            ClassMapper.Clear()
                .Add(PreFixCls)
                .If($"{PreFixCls}-disabled", () => Disabled)
                .If($"{PreFixCls}-vertical", () => Parent.Vertical)
                .If($"{PreFixCls}-with-marks", () => Parent.Marks != null)
                .If($"{PreFixCls}-rtl", () => RTL);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                DomEventListener.AddShared<JsonElement>("window", "mousemove", OnMouseMove);
                DomEventListener.AddShared<JsonElement>("window", "mouseup", OnMouseUp);
            }

            base.OnAfterRender(firstRender);
        }

        protected override void Dispose(bool disposing)
        {
            DomEventListener.Dispose();
            Parent.RemoveRangeItem(this);
            base.Dispose(disposing);
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

        //private async void OnMouseDown(MouseEventArgs args)
        //{
        //    //// _sliderDom = await JsInvokeAsync<DomRect>(JSInteropConstants.GetBoundingClientRect, _slider);
        //    //_sliderDom = await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _slider);
        //    //decimal x = (decimal)args.ClientX;
        //    //decimal y = (decimal)args.ClientY;

        //    //_mouseDown = !Disabled
        //    //    && _sliderDom.clientLeft <= x && x <= _sliderDom.clientLeft + _sliderDom.clientWidth
        //    //    && _sliderDom.clientTop <= y && y <= _sliderDom.clientTop + _sliderDom.clientHeight;

        //    //_mouseDown = !Disabled;
        //    //TODO: check how it behaves when range item is disabled
        //    //if (HasAttachedEdgeWithGap && !Master)
        //    //{
        //    //    Master = true;
        //    //    AttachedItem.Slave = true;
        //    //}
        //}

        private void OnRangeItemClick(MouseEventArgs args)
        {
            if (!_isFocused)
            {
                SetFocus(true);
                Parent.SetRangeItemFocus(this, true);
            }
        }

        internal void SetFocus(bool isFocused)
        {
            _isFocused = isFocused;
            if (_isFocused)
            {
                _focusClass = "ant-multi-range-slider-track-focus";
                _leftFocusZIndex = "z-index: 1000;"; //just below default overlay zindex
                _rightFocusZIndex = "z-index: 1000;";
            }
            else
            {
                _focusClass = "";
                _leftFocusZIndex = "z-index: 900;";
                _rightFocusZIndex = "z-index: 900;";
            }
            //Console.WriteLine($"SetFocus {Id}, {_focusClass}");
        }

        private void OnDoubleClick(RangeEdge handle)
        {
            if (AttachedHandleNo == 0)
            {
                RangeItem candidate = handle == RangeEdge.Left ? Parent.GetLeftNeighbour(Id) : Parent.GetRightNeighbour(Id);
                if (candidate is null && !Parent.AllowOverlapping) //will be null when there are no other items or edge is closes to either Min or Max
                {
                    ResetAttached();
                    return;
                }

                if (candidate is not null //only needed when Parent.AllowOverlapping = true
                    &&
                        (handle == RangeEdge.Left && candidate.RightValue == LeftValue
                        ||
                        handle == RangeEdge.Right && candidate.LeftValue == RightValue)) //handles overlapping edges (neighboring)
                {
                    AttachedItem = candidate;
                    _hasAttachedEdge = true;
                    AttachedHandleNo = handle;
                    Master = true;
                    AttachedItem.Slave = true;
                    if (handle == RangeEdge.Left)
                    {
                        ChangeAttachedItem = () => AttachedItem.RightValue = this.LeftValue;
                    }
                    else
                    {
                        ChangeAttachedItem = () => AttachedItem.LeftValue = this.RightValue;
                    }
                    ApplyLockEdgeStyle(handle, true);
                    Console.WriteLine($"OnDoubleClick {Id}, handle {handle}, attached: {AttachedItem.LeftValue}-{AttachedItem.RightValue}");
                }
                else
                {
                    if (Parent.ItemRequestingAttach is null)
                    {
                        Parent.ItemRequestingAttach = this;
                        HandleNoRequestingAttaching = handle;
                        ApplyLockEdgeStyle(handle);
                        Console.WriteLine($"OnDoubleClick {Id}, handle {handle}, separated, requesting");
                    }
                    else
                    {
                        if (Parent.AllowOverlapping)
                        {
                            if (Parent.ItemRespondingToAttach is null && Parent.ItemRequestingAttach.Id != Id) //do not allow same item edge locks, use dragging
                            {
                                Parent.ItemRespondingToAttach = this;
                                Master = true;
                                Parent.ItemRequestingAttach.Slave = true;
                                HasAttachedEdgeWithGap = true;
                                Parent.ItemRequestingAttach.HasAttachedEdgeWithGap = true;
                                Parent.ItemRequestingAttach.AttachedHandleNo = Parent.ItemRequestingAttach.HandleNoRequestingAttaching;
                                AttachedHandleNo = handle;
                                AttachedItem = Parent.ItemRequestingAttach;
                                Parent.ItemRequestingAttach.AttachedItem = this;
                                if (handle != AttachedItem.AttachedHandleNo)
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
                                    AttachedItem.GapDistance = GapDistance;
                                    ApplyLockEdgeStyle(handle, true);
                                    Parent.ItemRequestingAttach.ApplyLockEdgeStyle(GetOppositeEdge(handle), true, true);
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
                                    AttachedItem.GapDistance = GapDistance;
                                    ApplyLockEdgeStyle(handle, true);
                                    Parent.ItemRequestingAttach.ApplyLockEdgeStyle(handle, true, true);
                                }
                                Console.WriteLine($"OnDoubleClick {Id}, handle {handle}, separated, matching");
                            }
                            else 
                            {
                                ResetAttached(true);
                            }
                        }
                        else
                        {
                            //allow attaching only neighbors 
                            if (Parent.ItemRespondingToAttach is null
                                &&
                                Parent.ItemRequestingAttach.HandleNoRequestingAttaching != handle //make sure connected edges are not the same edges
                                &&
                                    (handle == RangeEdge.Left && Parent.ItemRequestingAttach.Id == Parent.GetLeftNeighbour(Id).Id //left 
                                    ||
                                    handle == RangeEdge.Right && Parent.ItemRequestingAttach.Id == Parent.GetRightNeighbour(Id).Id //right 
                                    )
                               )
                            {
                                Parent.ItemRespondingToAttach = this;
                                Master = true;
                                Parent.ItemRequestingAttach.Slave = true;
                                HasAttachedEdgeWithGap = true;
                                Parent.ItemRequestingAttach.HasAttachedEdgeWithGap = true;
                                Parent.ItemRequestingAttach.AttachedHandleNo = Parent.ItemRequestingAttach.HandleNoRequestingAttaching;
                                AttachedHandleNo = handle;
                                AttachedItem = Parent.ItemRequestingAttach;
                                Parent.ItemRequestingAttach.AttachedItem = this;
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
                                AttachedItem.GapDistance = GapDistance;
                                ApplyLockEdgeStyle(handle, true);
                                Parent.ItemRequestingAttach.ApplyLockEdgeStyle(GetOppositeEdge(handle), true, true);
                                Console.WriteLine($"OnDoubleClick {Id}, handle {handle}, separated, matching");
                            }
                            else 
                            {
                                ResetAttached(true);
                            }
                        }
                    }
                }
                return;
            }
            ResetAttached();
        }

        private static RangeEdge GetOppositeEdge(RangeEdge edge)
        {
            if (edge == RangeEdge.Left)
            {
                return RangeEdge.Right;
            }
            return RangeEdge.Left;
        }

        private void ApplyLockEdgeStyle(RangeEdge handle, bool locked = false, bool requestStateChange = false)
        {
            if (handle == RangeEdge.Left)
            {
                if (locked)
                {
                    _attachedLeftHandleClass = "ant-multi-range-slider-handle-lock ant-multi-range-slider-handle-lock-closed";
                    _leftHandleFill = _locked;
                }
                else
                {
                    _attachedLeftHandleClass = "ant-multi-range-slider-handle-lock ant-multi-range-slider-handle-lock-open";
                    _leftHandleFill = _unlocked;
                }
                _leftFocusZIndex = "z-index: 1010;";

            }
            else
            {
                if (locked)
                {
                    _attachedRightHandleClass = "ant-multi-range-slider-handle-lock ant-multi-range-slider-handle-lock-closed";
                    _rightHandleFill = _locked;
                }
                else
                {
                    _attachedRightHandleClass = "ant-multi-range-slider-handle-lock ant-multi-range-slider-handle-lock-open";
                    _rightHandleFill = _unlocked;
                }
                _rightFocusZIndex = "z-index: 1010;";
            }
            if (requestStateChange)
            {
                StateHasChanged();
            }
        }

        internal void ResetLockEdgeStyle(bool requestStateChange)
        {
            _attachedLeftHandleClass = "";
            _attachedRightHandleClass = "";
            _leftHandleFill = null;
            _rightHandleFill = null;
            if (requestStateChange)
            {
                StateHasChanged();
            }
        }

        private void ResetAttached(bool forceReset = false)
        {
            if (!_hasAttachedEdge && !forceReset)
            {
                return;
            }
            //reset all attached
            if (HasAttachedEdgeWithGap || forceReset)
            {
                if (Parent.ItemRequestingAttach is not null)
                {
                    Parent.ItemRequestingAttach.ResetLockEdgeStyle(Parent.ItemRequestingAttach.Id != Id);
                    Parent.ItemRequestingAttach.ChangeAttachedItem = default;
                    Parent.ItemRequestingAttach.HandleNoRequestingAttaching = 0;
                    Parent.ItemRequestingAttach.AttachedHandleNo = 0;
                    Parent.ItemRequestingAttach.AttachedItem = null;
                    Parent.ItemRequestingAttach.HasAttachedEdgeWithGap = false;
                    Parent.ItemRequestingAttach.GapDistance = 0;
                    Parent.ItemRequestingAttach.Master = false;
                    Parent.ItemRequestingAttach.Slave = false;
                    Parent.ItemRequestingAttach.AttachedItem = null;
                    Parent.ItemRequestingAttach = null;
                }
                if (Parent.ItemRespondingToAttach is not null)
                {
                    Parent.ItemRespondingToAttach.ResetLockEdgeStyle(Parent.ItemRespondingToAttach.Id != Id);
                    Parent.ItemRespondingToAttach.ChangeAttachedItem = default;
                    Parent.ItemRespondingToAttach.HandleNoRequestingAttaching = 0;
                    Parent.ItemRespondingToAttach.AttachedHandleNo = 0;
                    Parent.ItemRespondingToAttach.AttachedItem = null;
                    Parent.ItemRespondingToAttach.HasAttachedEdgeWithGap = false;
                    Parent.ItemRespondingToAttach.GapDistance = 0;
                    Parent.ItemRespondingToAttach.Master = false;
                    Parent.ItemRespondingToAttach.Slave = false;
                    Parent.ItemRespondingToAttach.AttachedItem = null;
                    Parent.ItemRespondingToAttach = null;

                }
            }
            else
            {
                if (AttachedItem is not null)
                {
                    AttachedItem.Slave = false;
                }
                ResetLockEdgeStyle(false);
                Master = false;
                AttachedItem = default;
                AttachedHandleNo = 0;
                ChangeAttachedItem = default;
            }
            SetFocus(_isFocused);
            HasAttachedEdgeWithGap = false;
            //Console.WriteLine($"OnDoubleClick {Id}, handle {handle}, attached: reset");
        }

        private double _trackedClientX;
        private double _trackedClientY;

        private void OnMouseDownEdge(MouseEventArgs args, bool right)
        {
            _mouseDown = !Disabled;
            SetFocus(true);
            _right = right;
            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
            _trackedClientX = args.ClientX;
            _trackedClientY = args.ClientY;
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
                Master = true;
                Slave = false;
                AttachedItem.Master = false;
                AttachedItem.Slave = true;
            }
        }

        private bool IsMoveInEdgeBoundary(JsonElement jsonElement)
        {
            double clientX = jsonElement.GetProperty("clientX").GetDouble();
            double clientY = jsonElement.GetProperty("clientY").GetDouble();

            return (clientX == _trackedClientX && clientY == _trackedClientY);
        }

        private async void OnMouseMove(JsonElement jsonElement)
        {
            if (_mouseDown)
            {
                _trackedClientX = jsonElement.GetProperty("clientX").GetDouble();
                _trackedClientY = jsonElement.GetProperty("clientY").GetDouble();
                _mouseMove = true;
                await CalculateValueAsync(Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble());

                OnChange?.Invoke(CurrentValue);
            }
        }

        private async void OnMouseUp(JsonElement jsonElement)
        {
            bool isMoveInEdgeBoundary = IsMoveInEdgeBoundary(jsonElement);
            if (_mouseDown)
            {
                _mouseDown = false;
                if (!isMoveInEdgeBoundary)
                {
                    await CalculateValueAsync(Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble());
                    OnAfterChange?.Invoke(CurrentValue);
                }
            }
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

        private async Task CalculateValueAsync(double clickClient)
        {
            _sliderDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, Ref);
            double sliderOffset = (double)(Parent.Vertical ? _sliderDom.AbsoluteTop : _sliderDom.AbsoluteLeft);
            double sliderLength = (double)(Parent.Vertical ? _sliderDom.ClientHeight : _sliderDom.ClientWidth);
            double handleNewPosition;
            if (_right)
            {
                if (_rightHandleDom == null)
                {
                    _rightHandleDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, _rightHandle);
                }
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

                double rightV = (Parent.MinMaxDelta * handleNewPosition / sliderLength) + Min;
                if (rightV < LeftValue)
                {
                    if (Parent.AllowOverlapping && _hasAttachedEdge) //push
                    {
                        RightValue = rightV;
                        LeftValue = rightV;
                    }
                    else if (!_hasAttachedEdge) //do not allow switching if locked with another range item
                    {
                        _right = false;
                        if (_mouseDown)
                            RightValue = _initialLeftValue;
                        LeftValue = rightV;
                        await FocusAsync(_leftHandle);
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
            else
            {
                if (_leftHandleDom == null)
                {
                    _leftHandleDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, _leftHandle);
                }
                if (_rightHandleDom == null)
                {
                    _rightHandleDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, _rightHandle);
                }
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

                double leftV = (Parent.MinMaxDelta * handleNewPosition / sliderLength) + Min;
                if (leftV > RightValue)
                {
                    if (Parent.AllowOverlapping && _hasAttachedEdge) //push
                    {
                        RightValue = leftV;
                        LeftValue = leftV;
                    }
                    else if (!_hasAttachedEdge) //do not allow switching if locked with another range item
                    {
                        _right = true;
                        if (_mouseDown)
                            LeftValue = _initialRightValue;
                        RightValue = leftV;
                        await FocusAsync(_rightHandle);
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
            ChangeAttachedItem?.Invoke();
        }

        internal void SetStyle()
        {
            var rightHandPercentage = (RightValue - Min) / Parent.MinMaxDelta;
            _rightHandleStyle = string.Format(CultureInfo.CurrentCulture, RightHandleStyleFormat, Formatter.ToPercentWithoutBlank(rightHandPercentage));
            var leftHandPercentage = (LeftValue - Min) / Parent.MinMaxDelta;
            _trackStyle = string.Format(CultureInfo.CurrentCulture, TrackStyleFormat, Formatter.ToPercentWithoutBlank(leftHandPercentage), Formatter.ToPercentWithoutBlank((RightValue - LeftValue) / Parent.MinMaxDelta));
            _leftHandleStyle = string.Format(CultureInfo.CurrentCulture, LeftHandleStyleFormat, Formatter.ToPercentWithoutBlank(leftHandPercentage));
            StateHasChanged();
        }



        protected override void OnValueChange((double, double) value)
        {
            base.OnValueChange(value);

            if (IsLeftAndRightChanged(value))
            {
                _leftValue = double.MinValue;
                _rightValue = double.MaxValue;
            }
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
