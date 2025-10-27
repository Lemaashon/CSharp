using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Form = System.Windows.Forms.Form;
using System.Drawing;

namespace Lema._Utilities
{
    public static class File_Utils
    {
        /// <summary>
        /// Sets the icon of the specified form using the provided icon path or a default icon.
        /// </summary>
        /// <remarks>If the <paramref name="iconPath"/> is null, the method uses a default icon embedded
        /// in the assembly. The method attempts to retrieve the icon from the specified path as an embedded
        /// resource.</remarks>
        /// <param name="form">The form whose icon is to be set. Cannot be null.</param>
        /// <param name="iconPath">The path to the icon resource. If null, a default icon is used.</param>
        public static void SetFormIcon(Form form, string iconPath = null)
        {
            iconPath ??= "Lema.Resources.Icons16.IconList16.ico";

            using (var stream = Global.Assembly.GetManifestResourceStream(iconPath))
            {
                if (stream is not null)
                {
                    form.Icon = new Icon(stream);
                }
            }
        }
    }
}
