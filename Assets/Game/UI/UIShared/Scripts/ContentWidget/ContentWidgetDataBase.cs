using System.Collections;

namespace UIShared
{
    public abstract class ContentWidgetDataBase
    {
    }

    public interface IContentWidgetView
    {
        bool Setup(ContentWidgetDataBase data);
        IEnumerator OnViewCreated();
    }
}
