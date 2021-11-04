﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;

namespace AntDesign
{
    public partial class MultiRangeGroup : AntDomComponentBase
    {
        private const string PreFixCls = "ant-multi-range-group";
        private List<MultiRangeSlider> _items = new();
        List<string> _keys = new();
        internal double _markHeight = 0;

        /// <summary>
        /// Used for rendering select options manually.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Tick mark of Slider, type of key must be number, and must in closed interval [min, max], each mark can declare its own style
        /// </summary>
        [Parameter]
        public RangeItemMark[] Marks { get; set; }

        internal bool IsFirst(MultiRangeSlider item)
        {
            if (_items.Count == 0)
            {
                return false;
            }
            return item.Id == _items[0].Id;
        }

        protected override void OnInitialized()
        {
            ClassMapper.Clear()
                .Add(PreFixCls);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && _items.Count > 0)
            {
                var firstTrackDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, _items.First()._railRef);
                var lastTrackDom = await JsInvokeAsync<HtmlElement>(JSInteropConstants.GetDomInfo, _items.Last()._railRef);
                _markHeight = (lastTrackDom.AbsoluteTop + lastTrackDom.ClientHeight) - firstTrackDom.AbsoluteTop;
                DebugHelper.WriteLine($"Calculated height: {_markHeight}");
                await InvokeAsync(StateHasChanged);

            }
            await base.OnAfterRenderAsync(firstRender);
        }

        internal bool IsLast(MultiRangeSlider item)
        {
            if (_items.Count == 0)
            {
                return false;
            }
            return item.Id == _items.Last().Id;
        }

        internal void AddMultiRangeSliderItem(MultiRangeSlider item)
        {
            if (Marks is not null && Marks.Any())
            {
                item.SetMarksFromParent(Marks);
            }
            _items.Add(item);
            if (_keys.Count < _items.Count)
            {
                _keys.Add(Guid.NewGuid().ToString());
            }
        }

        internal void RemoveMultiRangeSliderItem(MultiRangeSlider item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                _keys.RemoveAt(index);
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
    }
}
