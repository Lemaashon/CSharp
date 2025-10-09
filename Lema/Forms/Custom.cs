using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSSE.Forms
{
    public static class Custom
    {
    }
    public class FormResult
    {
        private bool cancelled;
        public bool ExampleCancelled 
        {
            get { return cancelled; }
            set { cancelled = value; } 
        }
        public object Object { get; set; }
        public List<object> Objects { get; set; } 
        public bool Cancelled { get; set; }
        public bool Valid { get; set; }
        public bool Affirmatice { get; set; }
    }
}
