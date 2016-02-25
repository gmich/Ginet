﻿using System.Collections.Generic;

namespace Ginet.Terminal
{
    public class CommandInfo
    {

        public enum ContinuationOption
        {
            Flush,
            Append
        }

        public ContinuationOption Continuation { get; set; }
        public string Command { get; set; }
        public IEnumerable<string> Arguments { get; set; } = new List<string>();
        
    }

}