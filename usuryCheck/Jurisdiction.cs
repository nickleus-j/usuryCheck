using System;
using System.Collections.Generic;
using System.Text;

namespace usuryCheck
{
    public class Jurisdiction
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>
        /// Maximum APR allowed by that jurisdiction, expressed as percent (e.g. 36.0 for 36%).
        /// Use 0 or negative to mean "no cap configured".
        /// </summary>
        public double MaxAprPercent { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }

    // wrapper for the combobox so we can hold an object and show text
    
}
