
using AntDesign.Core.Helpers;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;

namespace AntDesign
{
    public partial class MultiRangeSlider : AntInputComponentBase<IEnumerable<(double, double)>>
    {
        private const string PreFixCls = "ant-multi-range-slider";
        private bool _isInitialized = false;
        private bool _isAtfterFirstRender = false;
        internal RangeItem ItemRequestingAttach { get; set; }
        internal RangeItem ItemRespondingToAttach { get; set; }

        [Parameter]
        public bool AllowOverlapping { get; set; }

        /// <summary>
        /// If true, the slider will be vertical.
        /// </summary>
        [Parameter]
        public bool Vertical { get; set; }

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
                        _items.ForEach(x => x.SetStyle());
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


        [Parameter]
        public double VisibleMin
        {
            get => _visibleMin;
            set
            {
                if (value < Min)
                {
                    _visibleMin = Min;
                }
                else
                {
                    _visibleMin = value;
                }
            }
        }

        [Parameter]
        public double VisibleMax
        {
            get => _visibleMax; 
            set
            {
                if (value > Max)
                {
                    _visibleMax = Max;
                }
                else
                {
                    _visibleMax = value;
                }
            }
        }
        
        protected IEnumerable<(double, double)> _values;
        private double _visibleMin;
        private double _visibleMax;

        /// <summary>
        /// Get or set the selected values.
        /// </summary>
        [Parameter]
        public virtual IEnumerable<(double, double)> Values
        {
            get => _values;
            set
            {
                if (value != null && _values != null)
                {
                    var hasChanged = !value.SequenceEqual(_values);

                    if (!hasChanged)
                    {
                        return;
                    }

                    _values = value;
                }
                else if (value != null && _values == null)
                {
                    _values = value;
                }
                else if (value == null && _values != null)
                {
                    _values = default;
                }

                if (_isNotifyFieldChanged && Form?.ValidateOnChange == true)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        /// <summary>
        /// Used for rendering select options manually.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _trackWidth = GetRangeFullWidth();
            _isInitialized = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            ValidateParameter();

            ClassMapper.Clear()
                .Add(PreFixCls)
                //.If($"{PreFixCls}-disabled", () => Disabled)
                //.If($"{PreFixCls}-vertical", () => Vertical)
                //.If($"{PreFixCls}-with-marks", () => Marks != null)
                .If($"{PreFixCls}-rtl", () => RTL);
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
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                SortRangeItems();
                _isAtfterFirstRender = true;
            }
        }

        private async void OnMouseDown(MouseEventArgs args)
        {
            //_mouseDown = !Disabled;
        }

        private List<RangeItem> _items = new();
//        private Dictionary<string, (double minBoundary, double maxBoundary, RangeItem item)> _boundaries;
        private Dictionary<string, (Func<double> minBoundary, Func<double> maxBoundary, RangeItem item)> _boundaries1;
        private Dictionary<string, (RangeItem leftNeighbour, RangeItem rightNeighbour, RangeItem item)> _boundaries2;

        internal void AddRangeItem(RangeItem item)
        {
            _items.Add(item);
            //Console.WriteLine($"Added range item {_items.Count} with set left: {item.LeftValue}, right: {item.RightValue}");
        }

        internal double GetLeftBoundary(string id, int fromHandle, int attachedHandleNo)
        {
            //_boundaries2?[id].item?.HasAttachedEdgeWithGap ?? false
            if (AllowOverlapping)
            {
                if (_boundaries2?[id].item?.HasAttachedEdgeWithGap ?? false)
                {
                    double currentItemPulledEdge = _boundaries2[id].item.AttachedHandleNo == 1 ? _boundaries2[id].item.LeftValue : _boundaries2[id].item.RightValue;
                    double attachedItemPulledEdge = _boundaries2[id].item.AttachedItem.AttachedHandleNo == 1 ? _boundaries2[id].item.AttachedItem.LeftValue : _boundaries2[id].item.AttachedItem.RightValue;
                    if (attachedItemPulledEdge < currentItemPulledEdge)
                    {
                        return currentItemPulledEdge - attachedItemPulledEdge;
                    }
                    else
                    {
                        return Min; // Math.Min(Min, Min - _boundaries2[id].item.GapDistance);
                    }
                }
                else
                {
                    return Min;
                }
            }

            if (_isAtfterFirstRender && _boundaries2[id].leftNeighbour != default)
            {
                if (fromHandle == attachedHandleNo)
                {
                    if (_boundaries2[id].item.HasAttachedEdgeWithGap)
                    {
                        //looks like this never kicks in
                        //if (attachedHandleNo == 2)
                        //{
                        //    return _boundaries2[id].item.LeftValue;
                        //}
                        if (attachedHandleNo == 1)
                        {
                            return _boundaries2[id].leftNeighbour.LeftValue + _boundaries2[id].item.GapDistance;
                        }
                    }
                    else
                    {
                        if (attachedHandleNo == 2)
                        {
                            return _boundaries2[id].item.LeftValue; //attached is right edge, cannot go beyond current left edge, because the right neighbor (that is attached) cannot exceed beyond current left 
                        }
                        else if (attachedHandleNo == 1) //do not allow to exceed neighbor's edge
                        {
                            return _boundaries2[id].leftNeighbour.LeftValue;
                        }
                    }
                }
                else
                {
                    if (attachedHandleNo == 2)
                    {
                        return _boundaries2[id].leftNeighbour.RightValue;
                    }
                    else if (attachedHandleNo == 1) //do not allow to exceed neighbor's edge
                    {
                        return _boundaries2[id].leftNeighbour.LeftValue;
                    }
                    else
                    {
                        return _boundaries2[id].leftNeighbour.RightValue;
                    }
                }
            }
            return Min;
        }

        internal double GetRightBoundary(string id, int fromHandle, int attachedHandleNo)
        {
            if (AllowOverlapping)
            {
                if (_boundaries2?[id].item?.HasAttachedEdgeWithGap ?? false)
                {
                    double currentItemPulledEdge = _boundaries2[id].item.AttachedHandleNo == 1 ? _boundaries2[id].item.LeftValue : _boundaries2[id].item.RightValue;
                    double attachedItemPulledEdge = _boundaries2[id].item.AttachedItem.AttachedHandleNo == 1 ? _boundaries2[id].item.AttachedItem.LeftValue : _boundaries2[id].item.AttachedItem.RightValue;
                    if (attachedItemPulledEdge > currentItemPulledEdge)
                    {
                        return Max - (attachedItemPulledEdge - currentItemPulledEdge);
                    }
                    else
                    {
                        return Max; // Math.Min(Min, Min - _boundaries2[id].item.GapDistance);
                    }
                }
                else
                {
                    return Max;
                }
            }

            if (_isAtfterFirstRender && _boundaries2[id].rightNeighbour != default)
            {
                if (fromHandle == attachedHandleNo)
                {
                    if (_boundaries2[id].item.HasAttachedEdgeWithGap)
                    {
                        if (attachedHandleNo == 1)
                        {
                            return _boundaries2[id].item.RightValue;
                        }
                        if (attachedHandleNo == 2)
                        {
                            return _boundaries2[id].rightNeighbour.RightValue - _boundaries2[id].item.GapDistance; //in a gap situation, gap distance has to be accounted for
                        }
                    }
                    else
                    {
                        if (attachedHandleNo == 1)
                        {
                            return _boundaries2[id].rightNeighbour.LeftValue;
                        }
                        if (attachedHandleNo == 2)
                        {
                            return _boundaries2[id].rightNeighbour.RightValue;
                        }
                    }
                }
                else
                {
                    if (attachedHandleNo == 1)
                    {
                        return _boundaries2[id].item.RightValue;
                    }
                    if (attachedHandleNo == 2) //attached edge is right, so right boundary 
                    {
                        return _boundaries2[id].item.RightValue;
                    }
                    else
                    {
                        return _boundaries2[id].rightNeighbour.LeftValue;
                    }
                }
            }
            if (attachedHandleNo > 0) //because this is with attached, it's max is its own current Right
            {
                return _boundaries2[id].item.RightValue;
            }
            return Max;
        }

        internal RangeItem GetRightNeighbour(string id)
        {
            if (_isAtfterFirstRender && _boundaries2[id].rightNeighbour != default)
            {
                return _boundaries2[id].rightNeighbour;
            }
            return default;
        }

        internal RangeItem GetLeftNeighbour(string id)
        {
            if (_isAtfterFirstRender && _boundaries2[id].leftNeighbour != default)
            {
                return _boundaries2[id].leftNeighbour;
            }
            return default;
        }

        internal void SortRangeItems()
        {
            //TODO: allow adding IComparer or use default
            _items.Sort((s1, s2) => s1.Value.Item1.CompareTo(s2.Value.Item1));
            //_boundaries = new();
            _boundaries1 = new();
            _boundaries2 = new();
            //double minBoundary = Min;
            Func<double> minBoundary1 = () => Min;
            RangeItem leftNeighbour = default;
            string previousId = "";
            //(double minBoundary, double maxBoundary, RangeItem item) previousItem;
            (Func<double> minBoundary, Func<double> maxBoundary, RangeItem item) previousItem1;
            (RangeItem leftNeighbour, RangeItem rightNeighbour, RangeItem item) previousItem2;
            foreach (var item in _items)
            {
                if (previousId != string.Empty)
                {
                    //previousItem = _boundaries[previousId];
                    //previousItem.maxBoundary = item.Value.Item1;
                    //_boundaries[previousId] = previousItem;

                    previousItem1 = _boundaries1[previousId];
                    previousItem1.maxBoundary = () => item.Value.Item1;
                    _boundaries1[previousId] = previousItem1;

                    previousItem2 = _boundaries2[previousId];
                    previousItem2.rightNeighbour = item;
                    _boundaries2[previousId] = previousItem2;

                }
                previousId = item.Id;

                //_boundaries.Add(item.Id, (minBoundary, 0, item));
                //minBoundary = item.Value.Item2;

                _boundaries1.Add(item.Id, (minBoundary1, default, item));
                minBoundary1 = () => item.Value.Item2;

                _boundaries2.Add(item.Id, (leftNeighbour, default, item));
                leftNeighbour = item;

            }
            //previousItem = _boundaries[previousId];
            //previousItem.maxBoundary = Max;
            //_boundaries[previousId] = previousItem;

            previousItem1 = _boundaries1[previousId];
            previousItem1.maxBoundary = () => Max;
            _boundaries1[previousId] = previousItem1;

            previousItem2 = _boundaries2[previousId];
            previousItem2.rightNeighbour = default;
            _boundaries2[previousId] = previousItem2;

            //int i = 1;
            //foreach (var item in _boundaries1)
            //{
            //    Console.WriteLine($"Sorted range item {i} {item.Key} with set left: {item.Value.item.LeftValue} (no less than {item.Value.minBoundary}), right: {item.Value.item.RightValue} (no more than {item.Value.maxBoundary})");
            //    i++;
            //}
        }

        internal void RemoveRangeItem(RangeItem item) => _items.Remove(item);

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

        internal double MinMaxDelta => Max - Min;

        private string SetMarkPosition(double key)
        {
            return Formatter.ToPercentWithoutBlank((key - Min) / MinMaxDelta);
        }

        private string IsActiveMark(double key)
        {
            //TODO: should probably behave differently when multiple range items exists
            //    bool active = (Range && key >= LeftValue && key <= RightValue)
            //        || (!Range && key <= RightValue);

            //    return active ? "ant-multi-range-slider-dot-active" : string.Empty;
            return String.Empty;
        }

        private string _trackWidth = "";
        private string GetRangeFullWidth()
        {
            Console.WriteLine($"Min:{Min} Max:{Max} VisibleMin:{VisibleMin} VisibleMax:{VisibleMax}");
            if (Min >= VisibleMin && Max <= VisibleMax)
            {
                return "";
            }
            else
            {
                return $"width: {((Max - Min) / (VisibleMax - VisibleMin)) * 100}%";
            }
        }

        private RangeItem _focusedItem;
        internal void SetRangeItemFocus(RangeItem item, bool isFocused)
        {
            if (_focusedItem is not null)
            {
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

    }
}
