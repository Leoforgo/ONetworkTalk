public interface IWorkEngine<T> where T : class
{
void Add(T t);
void Start();
void Stop();
int GetLeftMessageCount();
}