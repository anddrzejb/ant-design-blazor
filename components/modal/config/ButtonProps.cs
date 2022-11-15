using Microsoft.AspNetCore.Components;
using OneOf;

namespace AntDesign
{
    /// <summary>
    /// button props
    /// </summary>
    public class ButtonProps
    {
        public bool Block { get; set; } = false;

        public bool Ghost { get; set; } = false;

        public bool Loading { get; set; } = false;

        public string Type { get; set; } = ButtonType.Default;

        public string Shape { get; set; } = null;

        public string Size { get; set; } = AntSizeLDSType.Default;

        public string Icon { get; set; }

        public bool Disabled { get; set; }

        /// <summary>
        /// Allow hiding of a button (or rather not rendering at all)
        /// </summary>
        public bool Visible { get; set; } = true;

        private bool? _danger;
        public bool? Danger { get => _danger; set => _danger = value; }

        internal bool IsDanger
        {
            get
            {
                if (Danger.HasValue) return Danger.Value;
                return false;
            }
        }

        public OneOf<string, RenderFragment>? ChildContent { get; set; } = null;
    }
}
