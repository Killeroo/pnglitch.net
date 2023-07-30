using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pnglitch
{
    internal class FilterFactory
    {
        private static Dictionary<FilterType, Filter> _filterDefinitions = new Dictionary<FilterType, Filter>();

        public static void Init()
        {
            RegisterDefinition(FilterType.None, new NoneFilter());
            RegisterDefinition(FilterType.Sub, new SubFilter());
            RegisterDefinition(FilterType.Up, new UpFilter());
            RegisterDefinition(FilterType.Average, new AverageFilter());
            RegisterDefinition(FilterType.Paeth, new PaethFilter());
        }

        private static void RegisterDefinition(FilterType @type, Filter definition)
        {
            if (_filterDefinitions.ContainsKey(type))
            {
                return;
            }

            _filterDefinitions.Add(type, definition);
        }

        public static void Encode(Scanline current, Scanline previous)
        {
            if (!_filterDefinitions.ContainsKey(current.FilterType))
            {
                return;
            }

            _filterDefinitions[current.FilterType].Encode(current, previous);
        }

        public static void Decode(Scanline current, Scanline previous)
        {
            if (!_filterDefinitions.ContainsKey(current.FilterType))
            {
                return;
            }

            _filterDefinitions[current.FilterType].Decode(current, previous);
        }
    }
    internal abstract class Filter
    {
        public abstract void Encode(Scanline current, Scanline previous);

        public abstract void Decode(Scanline current, Scanline previous);
    }

    internal class NoneFilter : Filter
    {
        public override void Decode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }

        public override void Encode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }
    }

    internal class SubFilter : Filter
    {
        public override void Decode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }

        public override void Encode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }
    }

    internal class UpFilter : Filter
    {
        public override void Decode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }

        public override void Encode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }
    }

    internal class AverageFilter : Filter
    {
        public override void Decode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }

        public override void Encode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }
    }

    internal class PaethFilter : Filter
    {
        public override void Decode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }

        public override void Encode(Scanline current, Scanline previous)
        {
            throw new NotImplementedException();
        }
    }
}
