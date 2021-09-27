using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AntDesign.core.Extensions;
using AntDesign.Core.Helpers;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AntDesign
{
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
        private bool _mouseDown;
        private bool _mouseMove;
        private bool _right = true;
        private bool _isInitialized = false;
        private double _initialLeftValue;
        private double _initialRightValue;
        private Tooltip _toolTipRight;
        private Tooltip _toolTipLeft;

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
        /// Make effect when <see cref="Marks"/> not null, true means containment and false means coordinative
        /// </summary>
        [Parameter]
        public bool Included { get; set; } = true;

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
                double candidate = Clamp(value, Parent.GetLeftBoundary(Id), Parent.GetRightBoundary(Id));
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
                double candidate = Clamp(value, LeftValue, Parent.GetRightBoundary(Id));

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

        private async void OnMouseDown(MouseEventArgs args)
        {
            //// _sliderDom = await JsInvokeAsync<DomRect>(JSInteropConstants.GetBoundingClientRect, _slider);
            //_sliderDom = await JsInvokeAsync<Element>(JSInteropConstants.GetDomInfo, _slider);
            //decimal x = (decimal)args.ClientX;
            //decimal y = (decimal)args.ClientY;

            //_mouseDown = !Disabled
            //    && _sliderDom.clientLeft <= x && x <= _sliderDom.clientLeft + _sliderDom.clientWidth
            //    && _sliderDom.clientTop <= y && y <= _sliderDom.clientTop + _sliderDom.clientHeight;

            _mouseDown = !Disabled;
        }

        private double _trackedClientX;
        private double _trackedClientY;

        private void OnMouseDownEdge(MouseEventArgs args, bool right)
        {
            _mouseDown = !Disabled;
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
                    _right = false;
                    if (_mouseDown)
                        RightValue = _initialLeftValue;
                    LeftValue = rightV;
                    await FocusAsync(_leftHandle);
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
                    _right = true;
                    if (_mouseDown)
                        LeftValue = _initialRightValue;
                    RightValue = leftV;
                    await FocusAsync(_rightHandle);
                }
                else
                {
                    LeftValue = leftV;
                }
            }
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
            LeftValue = value.Item1;
            RightValue = value.Item2;
        }

        private bool IsLeftAndRightChanged((double, double) value)
        {
            return (value.Item1 != LeftValue) && (value.Item2 != RightValue);
        }

        private (double, double) _value;
        private MultiRangeSlider _parent;

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
