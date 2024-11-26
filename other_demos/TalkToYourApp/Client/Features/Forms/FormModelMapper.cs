namespace TalkToYourApp.Client.Features.Forms;

public interface IFormModelMapper<T>
{
    public T MapModel(T source);
    public void MapModel(T source, T target);
}
