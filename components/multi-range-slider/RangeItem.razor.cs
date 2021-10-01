﻿using System;
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

    public partial class RangeItem : AntInputComponentBase<(double, double)>
    {
        private const string PreFixCls = "ant-multi-range-slider";
        private HtmlElement _sliderDom;
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
        private bool _mouseDownOnTrack;
        private bool _right = true;
        private bool _isInitialized = false;
        private double _initialLeftValue;
        private double _initialRightValue;
        private Tooltip _toolTipRight;
        private Tooltip _toolTipLeft;

        /// <summary>
        /// Used to figure out how much to move left & right when range is moved
        /// </summary>
        double _distanceToLeftHandle;
        double _distanceToRightHandle;


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
                        //if (Parent.Oversized)
                        //{
                        //    return "top: auto; bottom: calc({0} - 7px); transform: translateY(50%);";
                        //}
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
                        //if (Parent.Oversized)
                        //{
                        //    return "top: auto; bottom: calc({0} - 7px); transform: translateY(50%);";
                        //}
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
                        //if (Parent.Oversized)
                        //{
                        //    return "top: auto; height: {1}; bottom: calc({0} - 7px);";
                        //}
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

        //TODO: check with mixing of Disabled in Range and in parent
        /// <summary>
        /// If true, the slider will not be intractable
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
                if (_leftValue != value)
                {
                    ChangeLeftValue(candidate, value);
                }
            }
        }

        private void ChangeLeftValue(double value, double previousValue)
        {
            _leftValue = value;
            SetStyle();
            if (previousValue != CurrentValue.Item1)
                CurrentValue = (_leftValue, RightValue);
        }

        private double _rightValue = double.MaxValue;

        // the default non-range value
        internal double RightValue
        {
            get => _rightValue;
            set
            {
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
                if (_rightValue != value)
                {
                    ChangeRightValue(candidate, value);
                }
            }
        }

        private void ChangeRightValue(double value, double previousValue)
        {
            _rightValue = value;
            SetStyle();
            if (previousValue != CurrentValue.Item2)
                CurrentValue = (LeftValue, _rightValue);
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
                .Add($"{PreFixCls}-item")
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

        private async Task OnRangeItemClick(MouseEventArgs args)
        {
            //TODO: allow dragging ranges when 2 items are attached 
            if (!_isFocused)
            {
                SetFocus(true);
                Parent.SetRangeItemFocus(this, true);
            }
            _mouseDownOnTrack = !Disabled;
            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
            _trackedClientX = args.ClientX;
            _trackedClientY = args.ClientY;
            if (_toolTipRight != null)
            {
                _tooltipRightVisible = true;
                _toolTipRight.SetVisible(true, true);
                _tooltipLeftVisible = true;
                _toolTipLeft.SetVisible(true, true);
            }

            //evaluate clicked position in respect to each edge
            //(double sliderOffset, double sliderLength) = await GetSliderDimensions(Ref);
            (double sliderOffset, double sliderLength) = await GetSliderDimensions(Parent._railRef);
            double clickedValue = CalculateNewHandleValue(Parent.Vertical ? args.PageY : args.PageX, sliderOffset, sliderLength);
            _distanceToLeftHandle = clickedValue - LeftValue;
            _distanceToRightHandle = RightValue - clickedValue;
            Console.WriteLine($"Range item clicked. Click value: {clickedValue}, distance to Left:Right {_distanceToLeftHandle}:{_distanceToRightHandle}");
        }

        private async Task<(double, double)> GetSliderDimensions(ElementReference reference)
        {
            _sliderDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, reference);
            return ((double)(Parent.Vertical ? _sliderDom.AbsoluteTop  : _sliderDom.AbsoluteLeft),
                    (double)(Parent.Vertical ? _sliderDom.ClientHeight : _sliderDom.ClientWidth));

        }

        internal void SetFocus(bool isFocused)
        {
            _isFocused = isFocused;
            if (_isFocused)
            {
                _focusClass = $"{PreFixCls}-track-focus";
                if (!(_hasAttachedEdge && AttachedHandleNo == RangeEdge.Left))
                {
                    _leftFocusZIndex = "z-index: 1000;"; //just below default overlay zindex
                }
                if (!(_hasAttachedEdge && AttachedHandleNo == RangeEdge.Right))
                {
                    _rightFocusZIndex = "z-index: 1000;";
                }
            }
            else
            {
                _focusClass = "";
                if (!(_hasAttachedEdge && AttachedHandleNo == RangeEdge.Left))
                {
                    _leftFocusZIndex = "z-index: 900;";
                }
                if (!(_hasAttachedEdge && AttachedHandleNo == RangeEdge.Right))
                {
                    _rightFocusZIndex = "z-index: 900;";
                }
            }
        }

        private void OnDoubleClick(RangeEdge handle)
        {
            if (!_hasAttachedEdge)
            {
                RangeItem overlappingEdgeCandidate = handle == RangeEdge.Left ? Parent.GetLeftNeighbour(Id) : Parent.GetRightNeighbour(Id);
                if (overlappingEdgeCandidate is null && !Parent.AllowOverlapping) //will be null when there are no other items or edge is closes to either Min or Max
                {
                    ResetAttached();
                    return;
                }

                if (IsEdgeOverlapping(handle, overlappingEdgeCandidate)) 
                {
                    AttachOverlappingEdges(handle, overlappingEdgeCandidate);
                }
                else
                {
                    AttachNotOverlappingEdges(handle);
                }
                return;
            }
            ResetAttached();
        }

        private void AttachNotOverlappingEdges(RangeEdge handle)
        {
            if (!AttachFirstNotOverlappingEdge(handle))
            {
                if (Parent.AllowOverlapping)
                {
                    if (Parent.ItemRespondingToAttach is null && Parent.ItemRequestingAttach.Id != Id) //do not allow same item edge locks, use dragging
                    {
                        AttachSecondNotOverlappingEdge(handle,
                            handle == Parent.ItemRequestingAttach.HandleNoRequestingAttaching);
                    }
                    else
                    {
                        ResetAttached(true);
                    }
                }
                else
                {
                    if (AreEdgesNeighbours(handle))
                    {
                        AttachSecondNotOverlappingEdge(handle);
                    }
                    else
                    {
                        ResetAttached(true);
                    }
                }
            }
        }

        private bool AttachFirstNotOverlappingEdge(RangeEdge handle)
        {
            if (Parent.ItemRequestingAttach is null)
            {
                Parent.ItemRequestingAttach = this;
                HandleNoRequestingAttaching = handle;
                SetLockEdgeStyle(handle);
                return true;
            }
            return false;
        }

        private void AttachSecondNotOverlappingEdge(RangeEdge handle, bool isSameHandle = false)
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

        private void AttachOverlappingEdges(RangeEdge handle, RangeItem item)
        {
            AttachedItem = item;
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
            SetLockEdgeStyle(handle, true);
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
            focusIndex = "z-index: 1010;";
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
                ResetNotOverlapping(Parent.ItemRequestingAttach, Id);
                Parent.ItemRequestingAttach = null;
                ResetNotOverlapping(Parent.ItemRespondingToAttach, Id);
                Parent.ItemRespondingToAttach = null;
                //resort, because order could have been altered
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
                Master = false;
                AttachedItem = default;
                AttachedHandleNo = 0;
                ChangeAttachedItem = default;
            }
            SetFocus(_isFocused);
            HasAttachedEdgeWithGap = false;
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

        private double _trackedClientX;
        private double _trackedClientY;

        private void OnMouseDownEdge(MouseEventArgs args, bool right)
        {
            _mouseDown = !Disabled;
            SetFocus(true);
            Parent.SetRangeItemFocus(this, true);
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
                await CalculateValueAsync(Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble());

                OnChange?.Invoke(CurrentValue);
            }
            if (_mouseDownOnTrack)
            {
                _trackedClientX = jsonElement.GetProperty("clientX").GetDouble();
                _trackedClientY = jsonElement.GetProperty("clientY").GetDouble();
                await CalculateValuesAsync(Parent.Vertical ? jsonElement.GetProperty("pageY").GetDouble() : jsonElement.GetProperty("pageX").GetDouble());

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
            if (_mouseDownOnTrack)
            {
                _mouseDownOnTrack = false;
                if (HasAttachedEdgeWithGap)
                {
                    GapDistance = CalculateGapDistance();
                    AttachedItem.GapDistance = GapDistance;
                }
            }
            if (_toolTipRight != null)
            {
                if (_tooltipRightVisible != TooltipVisible)
                {
                    _tooltipRightVisible = TooltipVisible;
                    _toolTipRight.SetVisible(TooltipVisible, true);
                }

                if (_tooltipLeftVisible != TooltipVisible)
                {
                    _tooltipLeftVisible = TooltipVisible;
                    _toolTipLeft.SetVisible(TooltipVisible, true);
                }
            }

            _initialLeftValue = _leftValue;
            _initialRightValue = _rightValue;
        }

        private async Task CalculateValueAsync(double clickClient)
        {
            //_railRef
            (double sliderOffset, double sliderLength) = await GetSliderDimensions(Parent._railRef);
            if (_right)
            {
                await ProcessNewRightValue(clickClient, sliderOffset, sliderLength);
            }
            else
            {
                await ProcessNewLeftValue(clickClient, sliderOffset, sliderLength);
            }
            ChangeAttachedItem?.Invoke();
        }

        private async Task CalculateValuesAsync(double clickClient)
        {
            //_sliderDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, Ref);
            //double sliderOffset = (double)(Parent.Vertical ? _sliderDom.AbsoluteTop : _sliderDom.AbsoluteLeft);
            //double sliderLength = (double)(Parent.Vertical ? _sliderDom.ClientHeight : _sliderDom.ClientWidth);
            //(double sliderOffset, double sliderLength) = await GetSliderDimensions(Parent.Ref);
            (double sliderOffset, double sliderLength) = await GetSliderDimensions(Parent._railRef);

            double dragPosition = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
            double rightV = dragPosition + _distanceToRightHandle;
            double leftV = dragPosition - _distanceToLeftHandle;
            //evaluate if both rightV & leftV are within acceptable values
            double rightCandidate = Clamp(rightV, Parent.GetLeftBoundary(Id, RangeEdge.Right, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Right, AttachedHandleNo));
            double leftCandidate = Clamp(leftV, Parent.GetLeftBoundary(Id, RangeEdge.Left, AttachedHandleNo), Parent.GetRightBoundary(Id, RangeEdge.Left, AttachedHandleNo));
            if (leftCandidate != LeftValue && rightCandidate != RightValue)
            {
                ChangeLeftValue(leftCandidate, LeftValue);
                ChangeRightValue(rightCandidate, RightValue);
            }

        }

        private async Task ProcessNewRightValue(double clickClient, double sliderOffset, double sliderLength)
        {
            double rightV = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
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
                    SwitchTooltip(RangeEdge.Left);
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

        private async Task ProcessNewLeftValue(double clickClient, double sliderOffset, double sliderLength)
        {
            double leftV = CalculateNewHandleValue(clickClient, sliderOffset, sliderLength);
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
                    SwitchTooltip(RangeEdge.Right);
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
                    _toolTipRight.SetVisible(TooltipVisible, true);
                }
                _tooltipLeftVisible = true;
                return;
            }

            if (_tooltipLeftVisible != TooltipVisible)
            {
                _tooltipLeftVisible = TooltipVisible;
                _toolTipLeft.SetVisible(TooltipVisible, true);
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

        internal void SetStyle()
        {
            var rightHandPercentage = (RightValue - Min) / Parent.MinMaxDelta;
            var leftHandPercentage = (LeftValue - Min) / Parent.MinMaxDelta;
            string rightHandStyle;
            string leftHandStyle;
            string trackHeight;
            if (Parent.Vertical && Parent.Oversized)
            {
                rightHandStyle = MultiRangeSlider.GetOversizedVerticalCoordinate(rightHandPercentage);
                leftHandStyle = MultiRangeSlider.GetOversizedVerticalCoordinate(leftHandPercentage);
                //trackHeight = Parent.GetOversizedVerticalTrackSize((RightValue - LeftValue) / Parent.MinMaxDelta);
                trackHeight = MultiRangeSlider.GetOversizedVerticalTrackSize(leftHandPercentage, rightHandPercentage);
            }
            else
            {
                rightHandStyle = Formatter.ToPercentWithoutBlank(rightHandPercentage);
                leftHandStyle = Formatter.ToPercentWithoutBlank(leftHandPercentage);
                trackHeight = Formatter.ToPercentWithoutBlank((RightValue - LeftValue) / Parent.MinMaxDelta);
            }
            _rightHandleStyle = string.Format(CultureInfo.CurrentCulture, RightHandleStyleFormat, rightHandStyle);
            _trackStyle = string.Format(CultureInfo.CurrentCulture, TrackStyleFormat, leftHandStyle, trackHeight);
            _leftHandleStyle = string.Format(CultureInfo.CurrentCulture, LeftHandleStyleFormat, leftHandStyle);
            StateHasChanged();
        }

        protected override void OnValueChange((double, double) value)
        {
            base.OnValueChange(value);

            //if (!_hasAttachedEdge && IsLeftAndRightChanged(value))
            //{
            //    _leftValue = double.MinValue;
            //    _rightValue = double.MaxValue;
            //}
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
