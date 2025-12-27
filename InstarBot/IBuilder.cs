using JetBrains.Annotations;

namespace PaxAndromeda.Instar;

public interface IBuilder<out T>
{
	[UsedImplicitly]
	T Build();
}