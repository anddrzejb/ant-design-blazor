using OneOf;

namespace AntDesign
{
    public class RangeItemData : IRangeItemData
    {
        public RangeItemData() { }

        public RangeItemData((double first, double second) value)
        {
            Value = value;
        }

        public RangeItemData((double first, double second) value, string description) : this(value)
        {
            Description = description;
        }

        public RangeItemData((double first, double second) value, string description, string icon) : this(value, description)
        {
            Icon = icon;
        }

        public RangeItemData((double first, double second) value, string description, string icon, OneOf<Color, string> fontColor) : this(value, description, icon)
        {
            FontColor = fontColor;
        }

        public RangeItemData((double first, double second) value, string description, string icon, OneOf<Color, string> fontColor, OneOf<Color, string> color) : this(value, description, icon, fontColor)
        {
            Color = color;
        }

        public RangeItemData((double first, double second) value, string description, string icon, OneOf<Color, string> fontColor, OneOf<Color, string> color, OneOf<Color, string> focusColor) : this(value, description, icon, fontColor, color)
        {
            FocusColor = focusColor;
        }

        public RangeItemData((double first, double second) value, string description, string icon, OneOf<Color, string> fontColor, OneOf<Color, string> color, OneOf<Color, string> focusColor, OneOf<Color, string> focusBorderColor) : this(value, description, icon, fontColor, color, focusColor)
        {
            FocusBorderColor = focusBorderColor;
        }

        public (double first, double second) Value { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public OneOf<Color, string> FontColor { get; set; }
        public OneOf<Color, string> Color { get; set; }
        public OneOf<Color, string> FocusColor { get; set; }
        public OneOf<Color, string> FocusBorderColor { get; set; }
    }
}
