namespace Omu.ValueInjecter
{
    public interface IValueInjection
    {
        object Map(object source, object target);
    }
}