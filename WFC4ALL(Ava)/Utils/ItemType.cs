using Avalonia.Media;

namespace WFC4ALL.Utils; 

public class ItemType {

    private Color myColor;
    
    private ItemType(string sColor) {
        myColor = Color.Parse(sColor);
    }

    public Color getColor() {
        return myColor;
    }
    
    public static ItemType KeyLock = new("#FF0000");
}