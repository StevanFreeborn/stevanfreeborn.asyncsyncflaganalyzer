using System.Threading.Tasks;

public interface IDataService
{
  Task<string> GetDataAsync(int id, bool sync = false, bool useCache = true);
}

public class BusinessLogic
{
  private readonly IDataService _dataService;
  public BusinessLogic(IDataService dataService) { _dataService = dataService; }

  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    return await _dataService.GetDataAsync(17, sync);
  }
}
