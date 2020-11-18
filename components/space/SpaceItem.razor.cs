using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace AntDesign
{
    public partial class SpaceItem : ComponentBase
    {
        [CascadingParameter]
        public Space Parent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private static readonly Dictionary<string, string> _spaceSize = new()
        {
            ["small"] = "8",
            ["middle"] = "16",
            ["large"] = "24"
        };

        private string _marginStyle = "";

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Parent == null)
                return;

            var size = Parent.Size;
            var direction = Parent.Direction;

            var marginSize = size.IsIn("small", "middle", "large") ? _spaceSize[size] : size;

            _marginStyle = direction == "horizontal" ? $"margin-right:{(CssSizeLength)marginSize};" : $"margin-bottom:{(CssSizeLength)marginSize};";
        }
    }
}
