using System.ComponentModel;

namespace Omu.ValueInjecter
{

    public abstract class ValueInjection : IValueInjection
    {
        public object Map(object source, object target)
        {
            Inject(source, target);
            return target;
        }

        protected abstract void Inject(object source, object target);
    }
}