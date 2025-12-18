namespace PaxAndromeda.Instar;

public interface IBuilder<out T>
{
	T Build();
}