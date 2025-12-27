namespace PaxAndromeda.Instar.Services;

public interface IStartableService
{
	/// <summary>
	/// Starts the scheduled service.
	/// </summary>
	public Task Start();
}