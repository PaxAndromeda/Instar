namespace PaxAndromeda.Instar.Services;

public interface IStartableService
{
	/// <summary>
	/// Starts the scheduled service.
	/// </summary>
	Task Start();
}