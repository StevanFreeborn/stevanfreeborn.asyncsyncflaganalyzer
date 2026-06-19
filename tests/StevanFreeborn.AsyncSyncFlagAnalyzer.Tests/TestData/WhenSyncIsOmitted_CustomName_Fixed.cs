using System.Threading.Tasks;

public interface IDataService
{
  Task<string> GetMoreDataAsync(bool runSynchronously = false);
}

public class BusinessLogic
{
  private readonly IDataService _dataService;
  public BusinessLogic(IDataService dataService) { _dataService = dataService; }

  private async Task<string> GetFrobCoreAsync(bool runSynchronously)
  {
    return await _dataService.GetMoreDataAsync(runSynchronously);
  }
}
