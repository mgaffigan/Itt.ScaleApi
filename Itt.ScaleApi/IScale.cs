using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itt.ScaleApi
{
    public interface IScale
    {
        bool Stable { get; }
        decimal Weight { get; }
        event EventHandler<ScaleMeasurementEventArgs> WeightChanged;
    }

    public class ScaleMeasurementEventArgs : EventArgs
    {
        public ScaleMeasurementEventArgs(decimal weight, bool stable)
        {
            this.Weight = weight;
            this.Stable = stable;
        }

        public decimal Weight { get; }
        public bool Stable { get; }
    }
}
