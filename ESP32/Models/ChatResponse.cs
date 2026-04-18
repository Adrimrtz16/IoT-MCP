using System;
using System.Collections.Generic;
using System.Text;

namespace ESP32.Models
{
    public class ChatResponse
    {
        public string message { get; set; } = string.Empty;
        public string accion { get; set; } = string.Empty;
        public bool success { get; set; }
    }
}
