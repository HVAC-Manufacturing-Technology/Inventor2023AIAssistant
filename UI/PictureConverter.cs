using System.Windows.Forms;
using stdole;

namespace Inventor2023AIAssistant
{
    public class PictureConverter : AxHost
    {
        private PictureConverter() : base(string.Empty)
        {
        }

        public static IPictureDisp ImageToPictureDisp(System.Drawing.Image image)
        {
            return (IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}