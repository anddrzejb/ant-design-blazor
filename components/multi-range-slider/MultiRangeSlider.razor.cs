using AntDesign.Core.Helpers;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;
using AntDesign.JsInterop;
using System.Threading.Tasks;
using AntDesign.Select.Internal;
using System.Reflection;

namespace AntDesign
{
    public partial class MultiRangeSlider : AntInputComponentBase<IEnumerable<(double, double)>>
    {
        //TODO: performance - minimize re-renders

        //TODO: customize scrollbars: https://www.youtube.com/watch?v=lvKK2fs6h4I&t=36s&ab_channel=KevinPowell
        //TODO: switch between vertical & horizontal live (animation?)
        //TODO: fix multiple js errors on refersh 
        //TODO: test with & without tooltip & with forced tooltip
        //TODO: MAYBE: show 3rd/4th tooltip for attached edges when range is dragged
        internal const int VerticalOversizedTrackAdjust = 14;
        private const string PreFixCls = "ant-multi-range-slider";
        private bool _isAtfterFirstRender = false;
        private string _overflow = "display: inline;";
        private string _sizeType = "width";
        private string _railStyle = "";
        private double _boundaryAdjust = 0;
        private bool _isInitialized;
        //private double _itemAdjust = 0; //
        private bool _oversized;
        internal ElementReference _railRef;
        private ElementReference _scrollableAreaRef;
        List<string> _keys = new();
        internal RangeItem ItemRequestingAttach { get; set; }
        internal RangeItem ItemRespondingToAttach { get; set; }
        internal bool HasAttachedEdges { get; set; }
        internal double MinMaxDelta => Max - Min;
        internal bool Oversized { get => _oversized; set => _oversized = value; }
        internal double ItemAdjust { get; private set; }

        [Parameter]
        //TODO: consider passing an interface (RangeItem exposes to much stuff?)
        public Func<(RangeItem range, RangeEdge edge, double value), bool> OnEdgeMoving { get; set; }

        [Parameter]
        //TODO: consider passing an interface (RangeItem exposes to much stuff?)
        public EventCallback<(RangeItem range, RangeEdge edge, double value)> OnEdgeMoved { get; set; }

        [Parameter]
        //TODO: consider passing an interface (RangeItem exposes to much stuff?)
        public Func<(RangeItem left, RangeItem right), (bool allowAttaching, bool detachExistingOnCancel)> OnEdgeAttaching { get; set; }

        [Parameter]
        //TODO: consider passing an interface (RangeItem exposes to much stuff?)
        public EventCallback<(RangeItem left, RangeItem right)> OnEdgeAttached { get; set; }

        [Parameter]
        public Func<(RangeItem left, RangeItem right), bool> OnEdgeDetaching { get; set; }

        [Parameter]
        public EventCallback<(RangeItem left, RangeItem right)> OnEdgeDetached { get; set; }

        [Parameter]
        public bool AllowOverlapping { get; set; }

        /// <summary>
        /// If true, the slider will not be intractable
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public IEnumerable<IRangeItemData> Data { get; set; }


        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter]
        public EventCallback<IEnumerable<IRangeItemData>> DataChanged { get; set; }

        private bool _expandStepHasChanged;
        /// <summary>
        /// 
        /// </summary>
        [Parameter]
        public bool ExpandStep
        {
            get => _expandStep;
            set
            {

                if (_expandStep != value)
                {
                    _expandStepHasChanged = true;
                    _expandStep = value;
                }
            }
        }

        /// <summary>
        /// Useful only when <see cref="AllowOverlapping"/>is set to false.
        /// Does not allow edges to meet, because treats equal edge values
        /// as overlapping. 
        /// </summary>
        [Parameter]
        public bool EqualIsOverlap { get; set; }

        /// <summary>
        /// If true, the slider will be vertical.
        /// </summary>
        [Parameter]
        public bool Vertical
        {
            get => _vertical;
            set
            {
                _vertical = value;
                SetOrientationStyles();
            }
        }

        private void SetOrientationStyles()
        {
            if (_vertical)
            {
                //padding is 20px of the track width + "margin" of possible scroll;
                //without padding, vertical scroll hides most of the rendered elements
                if (Oversized)
                {
                    _overflow = "overflow-y: auto;overflow-x: hidden; padding-right: 8px; height: inherit;";
//                    _railStyle = $"height: calc(100% - {2 * VerticalOversizedTrackAdjust}px);top: {VerticalOversizedTrackAdjust}px;";
                }
                else
                {
                    _overflow = "display: inline;";
//                    _railStyle = "";
                }
                _railStyle = $"height: calc(100% - {2 * VerticalOversizedTrackAdjust}px);top: {VerticalOversizedTrackAdjust}px;";
                _sizeType = "height";
            }
            else
            {
                if (Oversized)
                {
                    _overflow = "overflow-x: auto;";
                }
                else
                {
                    _overflow = "display: inline;";
                }
                _sizeType = "width";
                _railStyle = "";
            }
        }

        /// <summary>
        /// Tick mark of Slider, type of key must be number, and must in closed interval [min, max], each mark can declare its own style
        /// </summary>
        [Parameter]
        public RangeItemMark[] Marks { get; set; }

        /// <summary>
        /// The maximum value the range slider
        /// </summary>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// The minimum value the range slider
        /// </summary>
        [Parameter]
        public double Min { get; set; } = 0;

        /// <summary>
        /// reverse the component
        /// </summary>
        private bool _reverse;

        [Parameter]
        public bool Reverse
        {
            get { return _reverse; }
            set
            {
                if (_reverse != value)
                {
                    _reverse = value;
                    //TODO: Optimize - move to either each RangeItem or OnParametersSet
                    if (_items.Any())
                    {
                        _items.ForEach(x => x.SetPositions());
                    }
                }
            }
        }

        /// <summary>
        /// The granularity the slider can step through values. Must greater than 0, and be divided by (<see cref="VisibleMax"/> - <see cref="VisibleMin"/>) . When <see cref="Marks"/> no null, <see cref="Step"/> can be null.
        /// </summary>
        private double? _step = 1;

        private int _precision;

        [Parameter]
        public double? Step
        {
            get { return _step; }
            set
            {
                _step = value;
                //no need to evaluate if no tooltip
                if (_step != null && _isTipFormatterDefault)
                {
                    char separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                    string[] number = _step.ToString().Split(separator);
                    if (number.Length > 1)
                    {
                        _precision = number[1].Length;
                        _tipFormatter = (d) => string.Format(CultureInfo.CurrentCulture, "{0:N02}", Math.Round(d, _precision));
                    }
                }
            }
        }

        [Parameter]
        public bool HasTooltip { get; set; } = true;

        /// <summary>
        /// Slider will pass its value to tipFormatter, and display its value in Tooltip
        /// </summary>
        private bool _isTipFormatterDefault = true;

        private Func<double, string> _tipFormatter = (d) => d.ToString(LocaleProvider.CurrentLocale.CurrentCulture);

        [Parameter]
        public Func<double, string> TipFormatter
        {
            get { return _tipFormatter; }
            set
            {
                _tipFormatter = value;
                _isTipFormatterDefault = false;
            }
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

        [Parameter]
        public double VisibleMin
        {
            get => _visibleMin;
            set
            {
                var hasChanged = value != _visibleMin;
                if (hasChanged)
                {
                    if (value < Min)
                    {
                        _visibleMin = Min;
                    }
                    else
                    {
                        _visibleMin = value;
                    }
                    Oversized = Min < _visibleMin || Max > _visibleMax;
                    SetOrientationStyles();
                    _trackSize = GetRangeFullSize();
                }

            }
        }

        [Parameter]
        public double VisibleMax
        {
            get => _visibleMax;
            set
            {
                var hasChanged = value != _visibleMax;
                if (hasChanged)
                {
                    if (value > Max)
                    {
                        _visibleMax = Max;
                    }
                    else
                    {
                        _visibleMax = value;
                    }
                    Oversized = Min < _visibleMin || Max > _visibleMax;
                    SetOrientationStyles();
                    _trackSize = GetRangeFullSize();
                }
            }
        }

        private double _visibleMin;
        private double _visibleMax;
        protected IEnumerable<(double, double)> _value;
        private IEnumerable<(double, double)> _valueClone = null;

        /// <summary>
        /// Get or set the selected values.
        /// </summary>
        [Parameter]
        public override IEnumerable<(double, double)> Value
        {
            get => _value;
            set
            {
                if (value != null && _valueClone != null)
                {
                    var hasChanged = value.Count() != _valueClone.Count(); //!value.SequenceEqual(_valueClone);

                    if (!hasChanged)
                    {
                        return;
                    }
                    if (ChildContent is null)
                    {
                        RebuildKeys(value, _valueClone); ;
                    }
                    _valueClone = _value.ToArray();
                    _ = OnValueChangeAsync(value);
                }
                else if (value != null && _valueClone == null)
                {
                    _value = value;
                    _valueClone = _value.ToArray();
                    _ = OnValueChangeAsync(value);
                }
                else if (value == null && _valueClone != null)
                {
                    _value = default;
                    _valueClone = default;
                    _ = OnValueChangeAsync(value);
                }

                if (_isNotifyFieldChanged && Form?.ValidateOnChange == true)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        /// <summary>
        ///     The Method is called every time if the value of the @bind-Values was changed by the two-way binding.
        /// </summary>
        protected async Task OnValueChangeAsync(IEnumerable<(double, double)> values)
        {
            if (!_isInitialized) // This is important because otherwise the initial value is overwritten by the EventCallback of ValueChanged and would be NULL.
            {
                return;
            }

            if (!_items.Any())
            {
                return;
            }

            if (values == null)
            {
                await ValueChanged.InvokeAsync(default);
                //OnSelectedItemsChanged?.Invoke(default);
                return;
            }

            //EvaluateValuesChangedOutsideComponent(values);

            //OnSelectedItemsChanged?.Invoke(SelectedOptionItems.Select(s => s.Item));
            await ValueChanged.InvokeAsync(Value);
        }

        async Task RangeItemDataChanged(IRangeItemData data, (double, double) value)
        {
            data.Value = value;
            await DataChanged.InvokeAsync(Data);
        }

        void RangeItemValueChanged(int index, (double, double) value)
        {
            //TODO: check if _value can be switched ot a List of tuples or other wrapped object, so it is passed as reference to RangeItem and can be used with @bind modifier
            var temp = _value.ToList();
            temp[index] = value;
            _value = temp;
            _ = OnValueChangeAsync(temp);
        }



        //TODO: taken from Select -> check if this applies
        /// <summary>
        ///     When bind-Values is changed outside of the component, then component
        ///     selected items have to be reselected according to new values passed.
        ///     TODO: (Perf) Consider using hash to identify if the passed values are different from currently selected.
        /// </summary>
        /// <param name="values">The values that need to be selected.</param>
        //private void EvaluateValuesChangedOutsideComponent(IEnumerable<(double, double)> values)
        //{
        //    var newSelectedItems = new List<(double, double)>();
        //    var deselectList = SelectedOptionItems.ToDictionary(item => item.Value, item => item);
        //    foreach (var value in values.ToList())
        //    {
        //        SelectOptionItem<TItemValue, TItem> result;
        //        if (SelectMode == SelectMode.Multiple)
        //        {
        //            result = SelectOptionItems.FirstOrDefault(x =>
        //                !x.IsSelected && EqualityComparer<TItemValue>.Default.Equals(x.Value, value));
        //            if (result != null && !result.IsDisabled)
        //            {
        //                result.IsSelected = true;
        //                SelectedOptionItems.Add(result);
        //            }

        //            deselectList.Remove(value);
        //        }
        //        else
        //        {
        //            result = SelectOptionItems.FirstOrDefault(x =>
        //                EqualityComparer<TItemValue>.Default.Equals(x.Value, value));
        //            if (result is null) //tag delivered from outside, needs to be added to the list of options
        //            {
        //                result = CreateSelectOptionItem(value.ToString(), true);
        //                result.IsSelected = true;
        //                AddedTags.Add(result);
        //                SelectOptionItems.Add(result);
        //                SelectedOptionItems.Add(result);
        //            }
        //            else if (result != null && !result.IsSelected && !result.IsDisabled)
        //            {
        //                result.IsSelected = true;
        //                SelectedOptionItems.Add(result);
        //            }

        //            deselectList.Remove(value);
        //        }
        //    }

        //    if (deselectList.Count > 0)
        //    {
        //        foreach (var item in deselectList)
        //        {
        //            item.Value.IsSelected = false;
        //            SelectedOptionItems.Remove(item.Value);
        //            if (item.Value.IsAddedTag)
        //            {
        //                SelectOptionItems.Remove(item.Value);
        //                AddedTags.Remove(item.Value);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Used for rendering select options manually.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _trackSize = GetRangeFullSize();
            _isInitialized = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            ValidateParameter();

            ClassMapper.Clear()
                .Add(PreFixCls)
                .If($"{PreFixCls}-disabled", () => Disabled)
                .If($"{PreFixCls}-vertical", () => Vertical)
                .If($"{PreFixCls}-vertical-oversized", () => Vertical && Oversized)
                .If($"{PreFixCls}-with-marks", () => Marks != null)
                .If($"{PreFixCls}-rtl", () => RTL);

            SetOrientationStyles();
            if (Step is not null)
            {
                if (EqualIsOverlap)
                {
                    _boundaryAdjust = Step.Value;
                }
                else
                {
                    _boundaryAdjust = 0;
                }
            }
            if (_expandStepHasChanged && Step is not null)
            {
                if (ExpandStep)
                {
                    ItemAdjust = (Step.Value / (Max - Min)) / 2d;
                }
                else
                {
                    ItemAdjust = 0;
                }
            }
        }

        private void ValidateParameter()
        {
            if (Step == null && Marks == null)
            {
                throw new ArgumentOutOfRangeException(nameof(Step), $"{nameof(Step)} can only be null when {nameof(Marks)} is not null.");
            }

            if (Step <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Step), "Must greater than 0.");
            }

            if (Step != null && (Max - Min) / Step % 1 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Step), $"Must be divided by ({Max} - {Min}).");
            }

            if (Data is not null && Value is not null)
            {
                throw new ArgumentException($"{nameof(Data)}, {nameof(Value)}", $"Either {nameof(Data)} or {nameof(Value)} parameters can be set. Not both.");
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                _isAtfterFirstRender = true;
                SortRangeItems();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && Oversized)
            {
                double x = 0, y = 0;
                if (Vertical)
                {
                    y = VisibleMin / 100d;
                }
                else
                {
                    x = VisibleMin / 100d;
                }
                await JsInvokeAsync(JSInteropConstants.DomMainpulationHelper.ScrollToPoint, _scrollableAreaRef, x, y, true);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private List<RangeItem> _items = new();
        private Dictionary<string, (RangeItem leftNeighbour, RangeItem rightNeighbour, RangeItem item)> _boundaries;

        internal void AddRangeItem(RangeItem item)
        {
            _items.Add(item);
            if (_keys.Count < _items.Count)
            {
                _keys.Add(Guid.NewGuid().ToString());
            }
            SortRangeItems();
        }

        internal void RemoveRangeItem(RangeItem item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                _keys.RemoveAt(index);
                SortRangeItems();
            }
        }

        private string GetOrAddKey(int index)
        {
            if (_keys.Count <= index)
            {
                string newKey = Guid.NewGuid().ToString();
                _keys.Add(newKey);
                return newKey;
            }
            return _keys[index];
        }

        private void RebuildKeys(IEnumerable<(double, double)> newValues, IEnumerable<(double, double)> oldValues)
        {
            if (_isInitialized)
            {
                for (int i = _items.Count-1; i >= 0; i--)
                {
                    _items[i].Dispose();

                }
                _keys.Clear();
            }
        }
        internal double GetLeftBoundary(string id, RangeEdge fromHandle, RangeEdge attachedHandle)
        {
            if (AllowOverlapping)
            {
                if (_boundaries?[id].item?.HasAttachedEdgeWithGap ?? false)
                {
                    GetPulledEdgesValues(id, out double currentItemPulledEdge, out double attachedItemPulledEdge);
                    if (attachedItemPulledEdge < currentItemPulledEdge)
                    {
                        return currentItemPulledEdge - attachedItemPulledEdge;
                    }
                }
                return Min;
            }

            if (_isAtfterFirstRender && _boundaries[id].leftNeighbour != default)
            {
                if (fromHandle == attachedHandle)
                {
                    if (_boundaries[id].item.HasAttachedEdgeWithGap)
                    {
                        if (attachedHandle == RangeEdge.Left)
                        {
                            return _boundaries[id].leftNeighbour.LeftValue + _boundaries[id].item.GapDistance;
                        }
                    }
                    else
                    {
                        if (attachedHandle == RangeEdge.Left) //do not allow to exceed neighbor's edge
                        {
                            return _boundaries[id].leftNeighbour.LeftValue;
                        }
                    }
                }
                else
                {
                    //for single edge, all except the closes to the Min
                    return _boundaries[id].leftNeighbour.RightValue + _boundaryAdjust;
                }
            }
            return Min;
        }

        internal double GetRightBoundary(string id, RangeEdge fromHandle, RangeEdge attachedHandleNo)
        {
            if (AllowOverlapping)
            {
                if (_boundaries?[id].item?.HasAttachedEdgeWithGap ?? false)
                {
                    GetPulledEdgesValues(id, out double currentItemPulledEdge, out double attachedItemPulledEdge);
                    if (attachedItemPulledEdge > currentItemPulledEdge)
                    {
                        return Max - (attachedItemPulledEdge - currentItemPulledEdge);
                    }
                }
                return Max;
            }

            if (_isAtfterFirstRender && _boundaries[id].rightNeighbour != default)
            {
                if (fromHandle == attachedHandleNo)
                {
                    if (_boundaries[id].item.HasAttachedEdgeWithGap)
                    {
                        if (attachedHandleNo == RangeEdge.Left)
                        {
                            return _boundaries[id].item.RightValue;
                        }
                        if (attachedHandleNo == RangeEdge.Right)
                        {
                            //in a gap situation, gap distance has to be accounted for
                            return _boundaries[id].rightNeighbour.RightValue - _boundaries[id].item.GapDistance;
                        }
                    }
                    else
                    {
                        if (attachedHandleNo == RangeEdge.Left)
                        {
                            return _boundaries[id].rightNeighbour.LeftValue;
                        }
                        else
                        {
                            //used only when range is dragged but 2 neighboring
                            //edges are attached & first range is dragged
                            return _boundaries[id].rightNeighbour.RightValue;
                        }
                    }
                }
                else
                {
                    //for single edge, all except the closes to the Max
                    return _boundaries[id].rightNeighbour.LeftValue - _boundaryAdjust;

                }
            }
            if (attachedHandleNo > 0) //because this is with attached, it's max is its own current Right
            {
                if (!_boundaries[id].item.IsRangeDragged)
                {
                    return _boundaries[id].item.RightValue;
                }
            }
            return Max;
        }

        private void GetPulledEdgesValues(string id, out double currentItemPulledEdge, out double attachedItemPulledEdge)
        {
            var currentItem = _boundaries[id].item;
            currentItemPulledEdge = GetPulledEdgeValue(currentItem);
            attachedItemPulledEdge = GetPulledEdgeValue(currentItem.AttachedItem);
        }

        private static double GetPulledEdgeValue(RangeItem item)
        {
            if (item.AttachedHandleNo == RangeEdge.Left)
            {
                return item.LeftValue;
            }
            else
            {
                return item.RightValue;
            }
        }
        internal RangeItem GetRightNeighbour(string id)
        {
            if (_isAtfterFirstRender && _boundaries?[id].rightNeighbour != default)
            {
                return _boundaries[id].rightNeighbour;
            }
            return default;
        }

        internal RangeItem GetLeftNeighbour(string id)
        {
            if (_isAtfterFirstRender && _boundaries?[id].leftNeighbour != default)
            {
                return _boundaries[id].leftNeighbour;
            }
            return default;
        }

        internal void SortRangeItems()
        {
            if (!_isAtfterFirstRender || _items.Count == 0)
            {
                return;
            }
            //TODO: allow adding IComparer or use default
            _items.Sort((s1, s2) =>
                {
                    var firstItemCompare = s1.Value.Item1.CompareTo(s2.Value.Item1);
                    if (firstItemCompare == 0)
                    {
                        return s1.Value.Item2.CompareTo(s2.Value.Item2);
                    }
                    return firstItemCompare;
                });

            _boundaries = new();
            RangeItem leftNeighbour = default;
            string previousId = "";
            (RangeItem leftNeighbour, RangeItem rightNeighbour, RangeItem item) previousItem;
            foreach (var item in _items)
            {
                if (previousId != string.Empty)
                {
                    previousItem = _boundaries[previousId];
                    previousItem.rightNeighbour = item;
                    _boundaries[previousId] = previousItem;

                }
                previousId = item.Id;

                _boundaries.Add(item.Id, (leftNeighbour, default, item));
                leftNeighbour = item;

            }

            previousItem = _boundaries[previousId];
            previousItem.rightNeighbour = default;
            _boundaries[previousId] = previousItem;
        }

        internal double GetNearestStep(double value)
        {
            if (Step.HasValue && (Marks == null || Marks.Length == 0))
            {
                return Math.Round(value / Step.Value, 0) * Step.Value;
            }
            else if (Step.HasValue)
            {
                return new double[2] { Math.Round(value / Step.Value) * Step.Value, Math.Round(value / Step.Value + 1) * Step.Value }.Union(Marks.Select(m => m.Key)).OrderBy(v => Math.Abs(v - value)).First();
            }
            else if (Marks.Length == 0)
            {
                return Min;
            }
            else
            {
                return Marks.Select(m => m.Key).OrderBy(v => Math.Abs(v - value)).First();
            }
        }

        private string SetMarkPosition(double key)
        {
            if (Vertical)
            {
                if (Reverse)
                {
                    return GetOversizedVerticalCoordinate(1 - (key - Min) / MinMaxDelta);
                }
                return GetOversizedVerticalCoordinate((key - Min) / MinMaxDelta);
            }
            if (Reverse)
            {
                return Formatter.ToPercentWithoutBlank(1 - ((key - Min) / MinMaxDelta));

            }
            return Formatter.ToPercentWithoutBlank((key - Min) / MinMaxDelta);
        }

        private string _trackSize = "";
        private string GetRangeFullSize()
        {
            if (Min >= VisibleMin && Max <= VisibleMax)
            {
                return "";
            }
            else
            {
                return $"{_sizeType}: {(Max - Min) / (VisibleMax - VisibleMin) * 100}%;";
            }
        }

        private RangeItem _focusedItem;
        private bool _vertical;
        private bool _expandStep;

        internal void SetRangeItemFocus(RangeItem item, bool isFocused)
        {
            if (_focusedItem is not null)
            {
                if (_focusedItem.Id == item.Id)
                {
                    return;
                }
                _focusedItem.SetFocus(false);
            }
            if (isFocused)
            {
                _focusedItem = item;
            }
            else
            {
                _focusedItem = null;
            }
        }

        /// <summary>
        /// When MultiRangeSlider is Vertical and is Oversized, special calculations are made, 
        /// there is a visual issue: Min and Max position are overflowing, so when an edge is set
        /// in the Min/Max, half of the edge is not visible due to overflowing set to hidden.
        /// 
        /// Current solution: make track smaller by a <see cref="VerticalOversizedTrackAdjust">number of pixels</see>.
        /// As a result, a relative calculation has to be performed to evaluate edge positions. 
        /// </summary>
        /// <param name="nominalPercentage">The percentage calculated for a point as it would be 
        /// used without compensating for visual issue.
        /// </param>
        /// <returns>css calc formula</returns>
        /// <see cref="GetOversizedVerticalTrackSize"/>
        internal static string GetOversizedVerticalCoordinate(double nominalPercentage)
        {
            var skew = GetOversizedVerticalSkew(nominalPercentage);
            return $"calc({Formatter.ToPercentWithoutBlank(nominalPercentage)} - ({skew} * {VerticalOversizedTrackAdjust}px))";
        }

        /// <summary>
        /// When MultiRangeSlider is Vertical and is Oversized, special calculations are made, 
        /// there is a visual issue: Min and Max position are overflowing, so when an edge is set
        /// in the Min/Max, half of the edge is not visible due to overflowing set to hidden.
        /// 
        /// Calculates the percentage of <see cref="VerticalOversizedTrackAdjust">number of pixels</see>.
        /// It will be applied to css calc formula.
        /// </summary>
        /// <param name="nominalPercentage">The percentage calculated for a point as it would be 
        /// used without compensating for visual issue.
        /// 
        /// It is a "pendulum algorithm" (don't know if such algorithm exists and if yes is this the correct name). 
        /// The logic here is that:
        /// 1. Rail is a range from 0% to 100%. 
        /// 2. Adjustment has to go from -100% at rail position = 0% to +100% at rail position = 100%. 
        /// 3. So if calculated from 0% => -100%, 1% => -98%, 2% => -96%, ... 50% => 0%, ..., 99% => 98%, 100% => 100%
        /// </param>
        /// <returns>percentage as fraction</returns>
        private static double GetOversizedVerticalSkew(double nominalPercentage)
        {
            double skew;
            if (nominalPercentage < 50)
            {
                skew = (2d * nominalPercentage) - 1d;
            }
            else
            {
                skew = (nominalPercentage - 0.5d) * 2d;
            }

            return skew;
        }

        /// <summary>
        /// When MultiRangeSlider is Vertical and is Oversized, special calculations are made, 
        /// there is a visual issue: Min and Max position are overflowing, so when an edge is set
        /// in the Min/Max, half of the edge is not visible due to overflowing set to hidden.
        /// 
        /// Current solution: make track smaller by a <see cref="VerticalOversizedTrackAdjust">number of pixels</see>.
        /// As a result, a relative calculation has to be performed to evaluate track size. 
        /// </summary>
        /// <param name="leftHandPercentage">The percentage calculated for the left edge as it would be 
        /// used without compensating for visual issu.
        /// </param>
        /// <param name="rightHandPercentage">The percentage calculated for the right edge it would be 
        /// used without compensating for visual issu.
        /// </param>/// 
        /// <returns>css calc formula</returns>
        /// <see cref="GetOversizedVerticalCoordinate"/>
        internal static string GetOversizedVerticalTrackSize(double leftHandPercentage, double rightHandPercentage)
        {
            var skewLeft = GetOversizedVerticalSkew(leftHandPercentage);
            var skewRight = GetOversizedVerticalSkew(rightHandPercentage);

            return $"calc(({Formatter.ToPercentWithoutBlank(rightHandPercentage)} - ({skewRight} * {VerticalOversizedTrackAdjust}px)) " +
                   $"- ({Formatter.ToPercentWithoutBlank(leftHandPercentage)} - ({skewLeft} * {VerticalOversizedTrackAdjust}px)))";
        }

        private string GetBasePosition() => Vertical ? "bottom" : "left";


    }
}
