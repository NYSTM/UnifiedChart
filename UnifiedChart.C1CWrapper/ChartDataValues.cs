using System.Collections.ObjectModel;

namespace UnifiedChart.C1CWrapper
{
    public sealed class ChartDataValues : Collection<double>
    {
        public void AddRange(params double[] values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public void CopyDataIn(IEnumerable<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            Clear();
            foreach (var value in values)
            {
                Add(value);
            }
        }

        protected override void SetItem(int index, double item)
        {
            while (index >= Count)
            {
                Add(0.0);
            }
            base.SetItem(index, item);
        }
    }
}
