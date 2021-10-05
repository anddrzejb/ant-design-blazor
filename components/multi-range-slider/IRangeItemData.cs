using OneOf;

namespace AntDesign
{
    public interface IRangeItemData
    {
        (double first, double second) Value { get; set; }
        string Description { get; set; }
        string Icon { get; set; }
        OneOf<Color, string> FontColor { get; set; }
        OneOf<Color, string> Color { get; set; }
        OneOf<Color, string> FocusColor { get; set; }
        OneOf<Color, string> FocusBorderColor { get; set; }
    }
}
