using System;
using System.Collections.Specialized;
using Fasterflect;
using Omu.ValueInjecter;

namespace MAF
{
    public class NameValueInjection : ValueInjection
    {
        protected override void Inject(object source, object target)
        {
            var nvc = source as NameValueCollection;
            if (nvc == null) { return; }
            foreach (string name in nvc)
            {
                var member = target.GetType().Property(name);//.Member(name);
                if (member == null)
                {
                    continue;
                }
                target.TrySetValue(name, TypeHelper.ChangeType(nvc[name], member.PropertyType));
            };
        }
    }
}