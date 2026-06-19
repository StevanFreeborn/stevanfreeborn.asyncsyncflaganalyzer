using System.Threading.Tasks;

public interface IDataService
{
  Task<string> GetDataAsync(bool sync = false);
  Task<string> GetMoreDataAsync(bool sync = false);
}

public class BusinessLogic
{
  private readonly IDataService _dataService;
  public BusinessLogic(IDataService dataService) { _dataService = dataService; }

  private async Task<string> GetFrobCoreAsync(bool sync)
  {
    var a = await _dataService.GetDataAsync(sync);
    var b = await _dataService.GetMoreDataAsync(sync);
    return a + b;
  }
}
