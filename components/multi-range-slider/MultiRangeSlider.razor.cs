
using AntDesign.Core.Helpers;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;
using OneOf.Types;

namespace AntDesign
{
    public partial class MultiRangeSlider : AntInputComponentBase<IEnumerable<(double, double)>>
    {
        private const string PreFixCls = "ant-multi-range-slider";
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
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            ValidateParameter();

            ClassMapper.Clear()
                .Add(PreFixCls)
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
                _isAtfterFirstRender = true;
                SortRangeItems();
            }
        }

        private async void OnMouseDown(MouseEventArgs args)
        {
            //_mouseDown = !Disabled;
        }

        private List<RangeItem> _items = new();
        private Dictionary<string, (RangeItem leftNeighbour, RangeItem rightNeighbour, RangeItem item)> _boundaries;

        internal void AddRangeItem(RangeItem item)
        {
            _items.Add(item);
            SortRangeItems();
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
                    return _boundaries[id].leftNeighbour.RightValue;
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
                            return _boundaries[id].rightNeighbour.RightValue - _boundaries[id].item.GapDistance; //in a gap situation, gap distance has to be accounted for
                        }
                    }
                    else
                    {
                        if (attachedHandleNo == RangeEdge.Left)
                        {
                            return _boundaries[id].rightNeighbour.LeftValue;
                        }
                    }
                }
                else
                {
                    //for single edge, all except the closes to the Max
                    return _boundaries[id].rightNeighbour.LeftValue; 

                }
            }
            if (attachedHandleNo > 0) //because this is with attached, it's max is its own current Right
            {
                return _boundaries[id].item.RightValue;
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

        internal void RemoveRangeItem(RangeItem item)
        {
            _items.Remove(item);
            SortRangeItems();
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
