using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChatAppFrontend
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string BackgroundColor { get; set; }
        public string ForegroundColor { get; set; }
        public HorizontalAlignment Alignment { get; set; }
    }

}
