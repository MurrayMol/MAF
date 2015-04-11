using System.ComponentModel;

namespace Omu.ValueInjecter
{
    public abstract class KnownSourceValueInjection<TSource> : IValueInjection
    {
        public object Map(object source, object target)
        {
            Inject((TSource) source, target);
            return target;
        }

        protected abstract void Inject(TSource source, object target);

    }
}