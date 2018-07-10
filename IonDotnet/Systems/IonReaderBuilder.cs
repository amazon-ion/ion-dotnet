using System;

namespace IonDotnet.Systems
{
    internal class IonReaderBuilder
    {
        private class MutableReaderBuilder : IonReaderBuilder
        {
            public MutableReaderBuilder()
            {
            }

            public MutableReaderBuilder(IonReaderBuilder that)
            {
            }

            public override IonReaderBuilder Immutable() => new IonReaderBuilder(this);

            public override IonReaderBuilder Mutable() => this;

            //do nothing
            protected override void MutationCheck()
            {
            }
        }

        private ICatalog _catalog;

        private IonReaderBuilder()
        {
        }

        private IonReaderBuilder(IonReaderBuilder that)
        {
            _catalog = that._catalog;
        }


        public ICatalog Catalog
        {
            get => _catalog;
            set
            {
                MutationCheck();
                _catalog = value;
            }
        }

        public virtual IonReaderBuilder Mutable() => Copy();

        public virtual IonReaderBuilder Immutable() => this;

        public IonReaderBuilder Copy() => new MutableReaderBuilder(this);

        public static IonReaderBuilder Standard => new MutableReaderBuilder();

        protected virtual void MutationCheck() => throw new InvalidOperationException("This builder is immutable");

        private ICatalog ValidateCatalog() => _catalog ?? new SimpleCatalog();
    }
}
